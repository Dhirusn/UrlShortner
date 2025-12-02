using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using UrlShortener.Data;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<ApplicationUser> _hasher = new();

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ApplicationUser> CreateUserAsync(string email, string username, string password)
        {
            var user = new ApplicationUser
            {
                Email = email,
                UserName = username,
                ProviderId = $"local:{Guid.NewGuid()}"
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return user;
        }

        public async Task<ApplicationUser?> ValidateUserAsync(string usernameOrEmail, string password)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x =>
                    x.Email == usernameOrEmail ||
                    x.UserName == usernameOrEmail);

            if (user == null) return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

            return result == PasswordVerificationResult.Success ? user : null;
        }

        public ClaimsPrincipal CreatePrincipal(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, "Local");
            return new ClaimsPrincipal(identity);
        }
    }
}
