using UrlShortener.Data.Models;
using UrlShortener.Models.ViewModels;

namespace UrlShortener.Services.Interfaces
{
    public interface IUrlShorteningService
    {
        Task<UrlMapping> CreateShortUrlAsync(string originalUrl, string userId, string customAlias = null, DateTime? expiresAt = null);
        Task<string> GetOriginalUrlAsync(string shortCode);
        Task<UrlDetailsViewModel> GetUrlDetailsAsync(string shortCode);
        Task<(List<UrlMapping> Items, int TotalCount)> GetUserUrlsAsync(string userId, int page = 1, int pageSize = 20);
        Task<bool> DeleteUrlAsync(string shortCode, string userId);
        Task TrackClickAsync(string shortCode, HttpContext context);
        Task<string> GenerateUniqueShortCodeAsync(string url, string customAlias = null);
    }
}
