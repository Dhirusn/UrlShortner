using StackExchange.Redis;
using System.Text.Json;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
        }

        // --------------------------------------------------------------------
        // GET URL FROM CACHE
        // --------------------------------------------------------------------
        public async Task<CachedUrl> GetUrlAsync(string shortCode)
        {
            try
            {
                string key = GetUrlKey(shortCode);
                var json = await _redisDb.StringGetAsync(key);

                if (json.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<CachedUrl>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis: Failed to get URL for {ShortCode}", shortCode);
                return null;
            }
        }

        // --------------------------------------------------------------------
        // STORE URL IN CACHE
        // --------------------------------------------------------------------
        public async Task SetUrlAsync(string shortCode, CachedUrl url, TimeSpan? expiry = null)
        {
            try
            {
                string key = GetUrlKey(shortCode);
                string json = JsonSerializer.Serialize(url, JsonOptions);

                await _redisDb.StringSetAsync(key, json, expiry, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis: Failed to set URL for {ShortCode}", shortCode);
            }
        }

        // --------------------------------------------------------------------
        // REMOVE URL FROM CACHE
        // --------------------------------------------------------------------
        public async Task RemoveUrlAsync(string shortCode)
        {
            try
            {
                string key = GetUrlKey(shortCode);
                await _redisDb.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis: Failed to remove URL for {ShortCode}", shortCode);
            }
        }

        // --------------------------------------------------------------------
        // RATE LIMITING (Sliding Window)
        // --------------------------------------------------------------------
        public async Task<bool> RateLimitAsync(string key, int limit, TimeSpan window)
        {
            try
            {
                var redisKey = GetRateLimitKey(key);

                // Increment request count
                var count = await _redisDb.StringIncrementAsync(redisKey);

                // If this is the first request, set window expiry
                if (count == 1)
                    await _redisDb.KeyExpireAsync(redisKey, window);

                // Allowed?
                return count <= limit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis: Rate limit error for key {Key}", key);

                // Fail open (allow) if Redis is down
                return true;
            }
        }

        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------
        private static string GetUrlKey(string shortCode) => $"url:{shortCode}";
        private static string GetRateLimitKey(string key) => $"ratelimit:{key}";
    }

    public class CachedUrl
    {
        public string OriginalUrl { get; set; }
        public string UserId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
