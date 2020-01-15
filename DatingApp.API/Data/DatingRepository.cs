using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data {
    public class DatingRepository : IDatingRepository {
        private readonly DataContext _context;
        public DatingRepository (DataContext context) {
            this._context = context;
        }
        public void Add<T> (T entity) where T : class {
            // In memory. Save all changes will persist the entity.
            _context.Add (entity);
        }

        public void Delete<T> (T entity) where T : class {
            // In memory. Save all changes will remove the entity.
            _context.Remove (entity);
        }

        public Task<Photo> GetPhoto (int id) {
            var photo = _context.Photos.FirstOrDefaultAsync (p => p.Id == id);

            return photo;
        }

        public async Task<User> GetUser (int id) {
            var user = await _context.Users.Include (p => p.Photos).FirstOrDefaultAsync (u => u.Id == id);

            return user;
        }

        public async Task<PagedList<User>> GetUsers (UserParams userParams) {
            var users = _context.Users.Include (p => p.Photos).OrderByDescending (u => u.LastActive).AsQueryable ();

            users = users.Where (u => u.Id != userParams.UserId);

            users = users.Where (u => u.Gender == userParams.Gender);

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                var minDateOfBirth = DateTime.Today.AddYears (-userParams.MaxAge - 1);
                var maxDateOfBirth = DateTime.Today.AddYears (-userParams.MinAge);

                users = users.Where (u => u.DateOfBirth >= minDateOfBirth && u.DateOfBirth <= maxDateOfBirth);
            }

            if (!String.IsNullOrEmpty (userParams.OrderBy)) {
                switch (userParams.OrderBy) {
                    case "created":
                        users = users.OrderByDescending (u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending (u => u.LastActive);
                        break;
                }

            }

            return await PagedList<User>.CreateAsync (users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }

        public async Task<Photo> GetMainPhotoForUser (int userId) {
            return await _context.Photos.Where (p => p.IsMain && p.UserId == userId).FirstOrDefaultAsync ();
        }
    }
}