using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers {
    [ServiceFilter (typeof (LogUserActivity))]
    [Route ("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase {
        private IDatingRepository _repo;
        private readonly IMapper _mapper;

        public UsersController (IDatingRepository repo, IMapper mapper) {
            this._mapper = mapper;
            this._repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers ([FromQuery] UserParams userParams) {
            var currentUserId = int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value);
            var userFromRepo = await this._repo.GetUser (currentUserId, true);

            userParams.UserId = currentUserId;
            if (string.IsNullOrEmpty (userParams.Gender)) {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            var users = await this._repo.GetUsers (userParams);

            var usersToReturn = this._mapper.Map<IEnumerable<UserForListDto>> (users);

            Response.AddPagination (users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok (usersToReturn);
        }

        [HttpGet ("{id}", Name = "GetUser")]
        public async Task<IActionResult> GetUser (int id) {

            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;
            var user = await this._repo.GetUser (id, isCurrentUser);

            var userToReturn = this._mapper.Map<UserForDetailedDto> (user);

            return Ok (userToReturn);
        }

        [HttpPut ("{id}")]
        public async Task<IActionResult> UpdateUser (int id, UserForUpdateDto userForUpdateDto) {
            if (id != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            var userFromRepo = await _repo.GetUser (id, true);

            _mapper.Map (userForUpdateDto, userFromRepo);

            if (await _repo.SaveAll ())
                return NoContent ();

            throw new Exception ($"Updating user with {id} failed on save");
        }

        [HttpPost ("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser (int id, int recipientId) {
            if (id != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }

            var like = await this._repo.GetLike (id, recipientId);
            if (like != null) {
                return BadRequest ("You  already like this user.");
            }

            var recipientUser = await this._repo.GetUser (recipientId, false);
            if (recipientUser == null) {
                return BadRequest ("User not found.");
            }

            like = new Like {
                LikerId = id,
                LikeeId = recipientId
            };

            _repo.Add (like);
            if (await _repo.SaveAll ())
                return Ok ();

            return BadRequest ("Failed to like user");
        }
    }
}