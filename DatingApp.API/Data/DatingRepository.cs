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

            if (userParams.Likers) {
                var userLikers = await GetUserLikes (userParams.UserId, userParams.Likers);
                users = users.Where (u => userLikers.Contains (u.Id));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes (userParams.UserId, userParams.Likers);
                users = users.Where (u => userLikees.Contains (u.Id));
            }

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

        private async Task<IEnumerable<int>> GetUserLikes (int id, bool likers) {
            var user = await _context.Users.Include (x => x.Likers).Include (x => x.Likees).FirstOrDefaultAsync (u => u.Id == id);

            if (likers) {
                return user.Likers.Where (u => u.LikeeId == id).Select (i => i.LikerId);
            } else {
                return user.Likees.Where (u => u.LikerId == id).Select (i => i.LikeeId);
            }
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }

        public async Task<Photo> GetMainPhotoForUser (int userId) {
            return await _context.Photos.Where (p => p.IsMain && p.UserId == userId).FirstOrDefaultAsync ();
        }

        public async Task<Like> GetLike (int userId, int recipientId) {
            return await _context.Likes.FirstOrDefaultAsync (
                l => l.LikerId == userId && l.LikeeId == recipientId
            );
        }

        public async Task<Message> GetMessage (int id) {
            return await _context.Messages.FirstOrDefaultAsync (m => m.Id == id);
        }

        public Task<PagedList<Message>> GetMessagesForUser (MessageParams messageParams) {
            var messages = _context.Messages
                .Include (m => m.Sender).ThenInclude (s => s.Photos)
                .Include (m => m.Recipient).ThenInclude (r => r.Photos)
                .AsQueryable ();

            switch (messageParams.MessageContainer) {
                case "Inbox":
                    messages = messages.Where (m => m.RecipientId == messageParams.UserId && m.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where (m => m.SenderId == messageParams.UserId && m.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where (m => m.RecipientId == messageParams.UserId && m.RecipientDeleted == false && m.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending (m => m.MessageSent);
            return PagedList<Message>.CreateAsync (messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread (int userId, int recipientId) {

            var messages = await _context.Messages
                .Include (m => m.Sender).ThenInclude (s => s.Photos)
                .Include (m => m.Recipient).ThenInclude (r => r.Photos)
                .Where (m => m.RecipientId == userId && m.RecipientDeleted == false && m.SenderId == recipientId ||
                    m.RecipientId == recipientId && m.SenderId == userId && m.SenderDeleted == false)
                .OrderByDescending (m => m.MessageSent)
                .ToListAsync ();

            return messages;

        }
    }
}