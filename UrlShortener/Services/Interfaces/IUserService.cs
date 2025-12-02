using System.Security.Claims;

namespace UrlShortener.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> CreateUserAsync(string email, string username, string password);
        Task<ApplicationUser?> ValidateUserAsync(string usernameOrEmail, string password);
        ClaimsPrincipal CreatePrincipal(ApplicationUser user);
    }
}
