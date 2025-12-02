using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Configuration;
using UrlShortener.Data;
using UrlShortener.Data.Models;
using UrlShortener.Models;
using UrlShortener.Models.ViewModels;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services
{
    public class UrlShorteningService : IUrlShorteningService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRedisCacheService _cacheService;
        private readonly ILogger<UrlShorteningService> _logger;
        private readonly AppSettings _settings;

      

        private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly Random _random = new();

        public UrlShorteningService(ApplicationDbContext dbContext, IRedisCacheService cacheService, ILogger<UrlShorteningService> logger, IOptions<AppSettings> settings)
        {
            _dbContext = dbContext;
            _cacheService = cacheService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<UrlMapping> CreateShortUrlAsync(string originalUrl, string userId, string customAlias = null, DateTime? expiresAt = null)
        {
            // Validate URL
            if (!IsValidUrl(originalUrl))
                throw new ArgumentException("Invalid URL format");

            string shortCode;
            Guid? userGuid = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);

            if (!string.IsNullOrEmpty(customAlias))
            {
                // Check if custom alias is available
                if (await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == customAlias))
                    throw new InvalidOperationException("Custom alias is already taken");

                shortCode = customAlias;
            }
            else
            {
                // Generate unique short code
                shortCode = await GenerateUniqueShortCodeAsync(originalUrl);
            }

            var expiresUtc = expiresAt?.ToUniversalTime();

            // Create URL mapping
            var urlMapping = new UrlMapping
            {
                ShortCode = shortCode,
                OriginalUrl = originalUrl,
                UserId = userGuid,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresUtc,
                Title = await ExtractPageTitleAsync(originalUrl)
            };


            await _dbContext.UrlMappings.AddAsync(urlMapping);
            await _dbContext.SaveChangesAsync();

            // Cache the result
            await _cacheService.SetUrlAsync(shortCode, new CachedUrl
            {
                OriginalUrl = originalUrl,
                UserId = userId,
                ExpiresAt = expiresAt
            });

            _logger.LogInformation("Created short URL: {ShortCode} for {OriginalUrl}", shortCode, originalUrl);

            return urlMapping;
        }

        public async Task<string> GetOriginalUrlAsync(string shortCode)
        {
            // Try cache first
            var cachedUrl = await _cacheService.GetUrlAsync(shortCode);
            if (cachedUrl != null)
            {
                // Check if expired
                if (cachedUrl.ExpiresAt.HasValue && cachedUrl.ExpiresAt < DateTime.UtcNow)
                {
                    await _cacheService.RemoveUrlAsync(shortCode);
                }
                else
                {
                    return cachedUrl.OriginalUrl;
                }
            }

            // Query database
            var urlMapping = await _dbContext.UrlMappings
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode && u.IsActive);

            if (urlMapping == null)
                return null;

            // Check expiration
            if (urlMapping.ExpiresAt.HasValue && urlMapping.ExpiresAt < DateTime.UtcNow)
            {
                urlMapping.IsActive = false;
                await _dbContext.SaveChangesAsync();
                return null;
            }

            // Update last accessed and increment counter (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    urlMapping.LastAccessedAt = DateTime.UtcNow;
                    urlMapping.ClickCount++;
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating click count for {ShortCode}", shortCode);
                }
            });

            // Cache for 24 hours or until expiration
            var cacheDuration = urlMapping.ExpiresAt.HasValue
                ? urlMapping.ExpiresAt.Value - DateTime.UtcNow
                : TimeSpan.FromHours(24);

            await _cacheService.SetUrlAsync(shortCode, new CachedUrl
            {
                OriginalUrl = urlMapping.OriginalUrl,
                UserId = urlMapping.UserId?.ToString(),
                ExpiresAt = urlMapping.ExpiresAt
            }, cacheDuration);

            return urlMapping.OriginalUrl;
        }

        public async Task<UrlDetailsViewModel> GetUrlDetailsAsync(string shortCode)
        {
            var urlMapping = await _dbContext.UrlMappings
                .Include(u => u.Clicks)
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if (urlMapping == null)
                return null;

            // Calculate daily stats for last 7 days
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var dailyClicks = await _dbContext.UrlClicks
                .Where(c => c.ShortCode == shortCode && c.ClickedAt >= sevenDaysAgo)
                .GroupBy(c => c.ClickedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Date, g => g.Count);

            // Get top referrers
            var topReferrers = await _dbContext.UrlClicks
                .Where(c => c.ShortCode == shortCode && !string.IsNullOrEmpty(c.Referrer))
                .GroupBy(c => c.Referrer)
                .Select(g => new ReferrerStat { Referrer = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .Take(10)
                .ToListAsync();

            return new UrlDetailsViewModel
            {
                ShortCode = urlMapping.ShortCode,
                OriginalUrl = urlMapping.OriginalUrl,
                ShortUrl = $"{GetBaseUrl()}/{urlMapping.ShortCode}",
                CreatedAt = urlMapping.CreatedAt,
                ExpiresAt = urlMapping.ExpiresAt,
                ClickCount = urlMapping.ClickCount,
                LastAccessedAt = urlMapping.LastAccessedAt,
                DailyClicks = dailyClicks,
                TopReferrers = topReferrers
            };
        }

        public async Task<(List<UrlMapping> Items, int TotalCount)> GetUserUrlsAsync(string userId, int page = 1, int pageSize = 20)
        {
            var userGuid = Guid.Parse(userId);

            var query = _dbContext.UrlMappings
                .Where(u => u.UserId == userGuid && u.IsActive)
                .OrderByDescending(u => u.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> DeleteUrlAsync(string shortCode, string userId)
        {
            var userGuid = Guid.Parse(userId);
            var urlMapping = await _dbContext.UrlMappings
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode && u.UserId == userGuid);

            if (urlMapping == null)
                return false;

            urlMapping.IsActive = false;
            await _dbContext.SaveChangesAsync();

            // Remove from cache
            await _cacheService.RemoveUrlAsync(shortCode);

            _logger.LogInformation("Deleted URL: {ShortCode} by user {UserId}", shortCode, userId);

            return true;
        }

        public async Task TrackClickAsync(string shortCode, HttpContext context)
        {
            try
            {
                var urlClick = new UrlClick
                {
                    ShortCode = shortCode,
                    ClickedAt = DateTime.UtcNow,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    Referrer = context.Request.Headers["Referer"].ToString(),
                    CountryCode = GetCountryFromIp(context.Connection.RemoteIpAddress?.ToString()),
                    DeviceType = GetDeviceType(context.Request.Headers["User-Agent"].ToString())
                };

                await _dbContext.UrlClicks.AddAsync(urlClick);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking click for {ShortCode}", shortCode);
            }
        }

        public async Task<string> GenerateUniqueShortCodeAsync(string url, string customAlias = null)
        {
            if (!string.IsNullOrEmpty(customAlias))
                return customAlias;

            string shortCode;
            int attempts = 0;

            do
            {
                if (attempts == 0)
                {
                    // First attempt: hash-based for same URL -> same code
                    shortCode = GenerateHashBasedCode(url, 7);
                }
                else
                {
                    // Subsequent attempts: random
                    shortCode = GenerateRandomCode(7 + attempts);
                }

                attempts++;

                // Check if exists in database
                var exists = await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == shortCode);
                if (!exists)
                {
                    // Also check cache
                    var cached = await _cacheService.GetUrlAsync(shortCode);
                    if (cached == null)
                        return shortCode;
                }

                if (attempts >= 5)
                    throw new InvalidOperationException("Failed to generate unique short code after multiple attempts");

            } while (true);
        }

        private string GenerateRandomCode(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Base62Chars[_random.Next(Base62Chars.Length)];
            }
            return new string(chars);
        }

        private string GenerateHashBasedCode(string input, int length)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            var numericValue = BitConverter.ToInt64(hash, 0);
            numericValue = Math.Abs(numericValue);

            var code = new char[length];
            for (int i = 0; i < length; i++)
            {
                code[i] = Base62Chars[(int)(numericValue % 62)];
                numericValue /= 62;
            }
            return new string(code);
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private async Task<string> ExtractPageTitleAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(3);
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var titleMatch = System.Text.RegularExpressions.Regex.Match(html, @"<title>(.*?)</title>");
                    if (titleMatch.Success)
                        return titleMatch.Groups[1].Value.Trim();
                }
            }
            catch
            {
                // Ignore errors - title extraction is optional
            }
            return null;
        }

        private string GetBaseUrl()
        {
            return _settings.BaseUrl;
        }

        private string GetCountryFromIp(string ipAddress)
        {
            // Implement IP to country lookup (could use a service or local database)
            return "US"; // Placeholder
        }

        private string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
                return "mobile";
            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "tablet";
            return "desktop";
        }
    }
}
