using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrlShortener.Data;
using UrlShortener.Data.Models;

namespace UrlShortener.Services
{
    public interface IOAuthService
    {
        Task<string> ProcessOAuthUserAsync(ClaimsPrincipal externalPrincipal);
        Task<ClaimsPrincipal> CreateApplicationClaimsAsync(string userId);
    }

    public class OAuthService : IOAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<OAuthService> _logger;

        public OAuthService(ApplicationDbContext dbContext, ILogger<OAuthService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> ProcessOAuthUserAsync(ClaimsPrincipal externalPrincipal)
        {
            var provider = externalPrincipal.FindFirstValue("provider") ??
                         externalPrincipal.FindFirstValue(ClaimTypes.AuthenticationMethod);

            var providerId = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = externalPrincipal.FindFirstValue(ClaimTypes.Email);
            var name = externalPrincipal.FindFirstValue(ClaimTypes.Name);
            var avatar = externalPrincipal.FindFirstValue("picture") ??
                        externalPrincipal.FindFirstValue("avatar_url");

            // Find existing user
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderId == providerId);

            if (user == null)
            {
                // Check if email exists with different provider
                if (!string.IsNullOrEmpty(email))
                {
                    user = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (user != null)
                    {
                        // Link this provider to existing account
                        user.Provider = provider;
                        user.ProviderId = providerId;
                        user.LastLoginAt = DateTime.UtcNow;
                    }
                }

                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        Name = name,
                        AvatarUrl = avatar,
                        Provider = provider,
                        ProviderId = providerId,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    await _dbContext.Users.AddAsync(user);
                }
            }
            else
            {
                // Update existing user
                user.LastLoginAt = DateTime.UtcNow;
                user.AvatarUrl = avatar ?? user.AvatarUrl;
                user.Name = name ?? user.Name;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("User {UserId} logged in via {Provider}", user.Id, provider);

            return user.Id.ToString();
        }

        public async Task<ClaimsPrincipal> CreateApplicationClaimsAsync(string userId)
        {
            var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
                throw new InvalidOperationException("User not found");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim("avatar_url", user.AvatarUrl ?? ""),
                new Claim("provider", user.Provider ?? "")
            };

            var identity = new ClaimsIdentity(claims, "Application");
            return new ClaimsPrincipal(identity);
        }
    }
}