using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersControllers : ControllerBase
    {
        private IDatingRepository _repo;

        public UsersControllers(IDatingRepository repo)
        {
            this._repo = repo;
        }


        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await this._repo.GetUsers();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var users = await this._repo.GetUser(id);
            return Ok(users);
        }
    }
}