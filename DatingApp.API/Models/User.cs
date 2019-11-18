using System;

namespace DatingApp.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public String Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
    }
}