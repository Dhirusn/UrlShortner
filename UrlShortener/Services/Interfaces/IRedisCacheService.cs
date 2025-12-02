namespace UrlShortener.Services.Interfaces
{
    public interface IRedisCacheService
    {
        Task<CachedUrl> GetUrlAsync(string shortCode);
        Task SetUrlAsync(string shortCode, CachedUrl url, TimeSpan? expiry = null);
        Task RemoveUrlAsync(string shortCode);
        Task<bool> RateLimitAsync(string key, int limit, TimeSpan window);
    }
}
