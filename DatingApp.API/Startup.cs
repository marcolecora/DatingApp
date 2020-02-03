using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices (IServiceCollection services) {
            /* Data source. */
            services.AddDbContext<DataContext> (x => {
                x.UseLazyLoadingProxies ();
                x.UseSqlite (Configuration.GetConnectionString ("DefaultConnection"));
            });

            ConfigureServices (services);
        }

        public void ConfigureProductionServices (IServiceCollection services) {
            /* Data source. */
            services.AddDbContext<DataContext> (
                x => {
                    x.UseLazyLoadingProxies ();
                    x.UseMySql (Configuration.GetConnectionString ("DefaultConnection"));
                });

            ConfigureServices (services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            IdentityBuilder builder = services.AddIdentityCore<User> (
                opt => {
                    opt.Password.RequireDigit = false;
                    opt.Password.RequiredLength = 4;
                    opt.Password.RequireNonAlphanumeric = false;
                    opt.Password.RequireUppercase = false;
                }
            );

            builder = new IdentityBuilder (builder.UserType, typeof (Role), builder.Services);
            builder.AddEntityFrameworkStores<DataContext> ();
            builder.AddRoleValidator<RoleValidator<Role>> ();
            builder.AddRoleManager<RoleManager<Role>> ();
            builder.AddSignInManager<SignInManager<User>> ();

            /* Authentication. */
            services.AddAuthentication (JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer (options => {
                    options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey (Encoding.ASCII.GetBytes (Configuration.GetSection ("AppSettings:Token").Value)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                    };
                });

            services.AddAuthorization (
                opt => {
                    opt.AddPolicy ("RequireAdminRole", policy => policy.RequireRole ("Admin"));
                    opt.AddPolicy ("ModeratePhotoRole", policy => policy.RequireRole ("Admin", "Moderator"));
                    opt.AddPolicy ("VipOnly", policy => policy.RequireRole ("VIP"));
                }
            );

            /* Controllers. */
            var policy = new AuthorizationPolicyBuilder ()
                .RequireAuthenticatedUser ()
                .Build ();

            services.AddControllers (
                    options => options.Filters.Add (new AuthorizeFilter (policy))
                )
                .SetCompatibilityVersion (CompatibilityVersion.Version_3_0)

                .AddNewtonsoftJson (
                    opt => opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );

            /* Cross Origin Resource Sharing. */
            services.AddCors ();
            services.Configure<CloudinarySettings> (Configuration.GetSection ("CloudinarySettings"));
            services.AddAutoMapper (typeof (DatingRepository).Assembly);

            /* Repositories. */
            // services.AddScoped<IAuthRepository, AuthRepository> ();
            services.AddScoped<IDatingRepository, DatingRepository> ();

            services.AddScoped<LogUserActivity> ();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            } else {
                app.UseExceptionHandler (
                    builder => {
                        builder.Run (async context => {
                            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                            var error = context.Features.Get<IExceptionHandlerFeature> ();
                            if (error != null) {
                                context.Response.AddApplicationError (error.Error.Message);
                                await context.Response.WriteAsync (error.Error.Message);
                            }
                        });
                    });
            }

            // app.UseHttpsRedirection();

            app.UseRouting ();

            app.UseAuthentication ();
            app.UseAuthorization ();

            app.UseCors (x => x.AllowAnyOrigin ().AllowAnyMethod ().AllowAnyHeader ());

            /* Allow web server to serve (default) static files.*/
            app.UseDefaultFiles ();
            app.UseStaticFiles ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
                endpoints.MapFallbackToController ("Index", "Fallback");
            });
        }
    }
}