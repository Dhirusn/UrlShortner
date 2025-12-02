using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using UrlShortener.Application.Configuration;
using UrlShortener.Data;
using UrlShortener.Data.Models;
using UrlShortener.Models;
using UrlShortener.Models.ViewModels;
using UrlShortener.Services.Interfaces;
using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace UrlShortener.Services
{
    public class UrlShorteningService : IUrlShorteningService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRedisCacheService _cacheService;
        private readonly ILogger<UrlShorteningService> _logger;
        private readonly AppSettings _settings;
        private static readonly char[] Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();

        public UrlShorteningService(ApplicationDbContext dbContext, IRedisCacheService cacheService, ILogger<UrlShorteningService> logger, IOptions<AppSettings> settings)
        {
            _dbContext = dbContext;
            _cacheService = cacheService;
            _logger = logger;
            _settings = settings.Value;
        }



        private string ToBase62(byte[] bytes, int length)
        {
            // Convert bytes to a positive BigInteger
            var extended = bytes.Concat(new byte[] { 0 }).ToArray();
            var bi = new BigInteger(extended);

            if (bi < 0) bi = BigInteger.Negate(bi);

            var sb = new StringBuilder();
            var baseLen = Base62Chars.Length;

            while (sb.Length < length)
            {
                if (bi == 0)
                {
                    // pad with random chars if the BigInteger runs out
                    sb.Append(Base62Chars[GetRandomInt(0, baseLen)]);
                }
                else
                {
                    var rem = (int)(bi % baseLen);
                    sb.Append(Base62Chars[rem]);
                    bi = bi / baseLen;
                }
            }

            // Return the first 'length' characters as the code
            return new string(sb.ToString().Take(length).ToArray());
        }

        private int GetRandomInt(int minInclusive, int maxExclusive)
        {
            var diff = (long)maxExclusive - minInclusive;
            if (diff <= 0) return minInclusive;
            // 32-bit random
            var uint32Buffer = new byte[4];
            _random.GetBytes(uint32Buffer);
            var rand = BitConverter.ToUInt32(uint32Buffer, 0);
            return (int)(minInclusive + (rand % diff));
        }

        private string GenerateHashBasedCode(string input, int length)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return ToBase62(hash, length);
        }

        private string GenerateRandomCode(int minLength = 5, int maxLength = 10, string baseHint = null)
        {
            if (minLength < 1) minLength = 5;
            if (maxLength < minLength) maxLength = minLength;

            var targetLength = GetRandomInt(minLength, maxLength + 1); // inclusive max
            var sb = new StringBuilder();

            // If there's a baseHint, try to incorporate a short snippet (1-3 chars) from it
            if (!string.IsNullOrEmpty(baseHint))
            {
                var sanitizedHint = new string(baseHint
                    .Where(c => Base62Chars.Contains(c))
                    .ToArray());

                if (sanitizedHint.Length > 0)
                {
                    var maxSnippet = Math.Min(3, sanitizedHint.Length);
                    var snippetLen = GetRandomInt(1, maxSnippet + 1);
                    var start = GetRandomInt(0, sanitizedHint.Length - snippetLen + 1);
                    var snippet = sanitizedHint.Substring(start, snippetLen);

                    // Decide whether to prefix, suffix, or embed
                    var insertPosition = GetRandomInt(0, targetLength - snippetLen + 1);
                    // fill up to insertPosition
                    while (sb.Length < insertPosition)
                        sb.Append(Base62Chars[GetRandomInt(0, Base62Chars.Length)]);

                    sb.Append(snippet);
                }
            }

            // Fill the rest with random base62 chars
            while (sb.Length < targetLength)
                sb.Append(Base62Chars[GetRandomInt(0, Base62Chars.Length)]);

            // If somehow longer, trim
            return sb.ToString(0, targetLength);
        }

        private char GetRandomChar()
        {
            return Base62Chars[GetRandomInt(0, Base62Chars.Length)];
        }

        public async Task<string> GenerateUniqueShortCodeAsync(string url, string customAlias = null)
        {
            if (!string.IsNullOrEmpty(customAlias))
                return customAlias;

            int attempts = 0;
            const int maxAttempts = 8; // more room to try unique variants

            do
            {
                string shortCode;
                if (attempts == 0)
                {
                    // deterministic first attempt using hash, length between 5-10 (choose 7 for stability)
                    var len = 7;
                    shortCode = GenerateHashBasedCode(url, len);
                }
                else
                {
                    // subsequent attempts: random 5-10 chars, optionally seed with url-derived hint
                    // use a short URL-derived hint to make similar URLs produce visually related codes
                    var hint = GenerateHashBasedCode(url, 3); // 3-char stable hint from hash
                    shortCode = GenerateRandomCode(5, 10, hint);
                }

                attempts++;

                var existsInDb = await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == shortCode);
                if (!existsInDb)
                {
                    var cached = await _cacheService.GetUrlAsync(shortCode);
                    if (cached == null)
                        return shortCode;
                }

                if (attempts >= maxAttempts)
                    throw new InvalidOperationException("Failed to generate unique short code after multiple attempts");

            } while (true);
        }

        private async Task<string> GenerateAvailableAliasAsync(string baseAlias)
        {
            // Ensure baseAlias is trimmed and within allowed characters (base62). If not, sanitize it.
            var sanitized = new string((baseAlias ?? string.Empty)
                .Where(c => Base62Chars.Contains(c))
                .ToArray());

            // If sanitized empty, just generate a random code (5-10 chars)
            if (string.IsNullOrEmpty(sanitized))
            {
                string fallback;
                do
                {
                    fallback = GenerateRandomCode(5, 10, null);
                } while (await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == fallback));

                return fallback;
            }

            // If sanitized length is already between 5-10, try using it as-is first.
            if (sanitized.Length >= 5 && sanitized.Length <= 10)
            {
                var attemptAlias = sanitized;
                var collisions = 0;
                while (await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == attemptAlias))
                {
                    // append a 1-2 char snippet from sanitized or random to keep it "common"
                    var suffix = GenerateRandomCode(1, 2, sanitized);
                    attemptAlias = (sanitized + suffix).Substring(0, Math.Min(10, sanitized.Length + suffix.Length));
                    collisions++;
                    if (collisions > 6)
                    {
                        // fallback to fully random but keep a hint of sanitized
                        attemptAlias = GenerateRandomCode(5, 10, sanitized);
                        break;
                    }
                }

                return attemptAlias;
            }
            else
            {
                // If sanitized too short or too long, craft an alias between 5-10 characters that includes part of sanitized
                var targetLen = GetRandomInt(5, 11);
                // choose snippet from sanitized
                var snippetLen = Math.Min(sanitized.Length, Math.Min(3, targetLen - 2));
                var snippet = sanitized.Substring(0, snippetLen);

                string attempt;
                int tries = 0;
                do
                {
                    // Create alias by combining snippet + random tail/prefix and trim/pad to targetLen
                    var tail = GenerateRandomCode(1, targetLen - snippetLen, snippet);
                    attempt = (snippet + tail).Substring(0, targetLen);

                    tries++;
                    if (tries > 10)
                    {
                        // broaden search: create random code with sanitized as hint
                        attempt = GenerateRandomCode(5, 10, sanitized);
                        break;
                    }
                } while (await _dbContext.UrlMappings.AnyAsync(u => u.ShortCode == attempt));

                return attempt;
            }
        }

        public async Task<UrlMapping> CreateShortUrlAsync(string originalUrl, string userId, string customAlias = null, DateTime? expiresAt = null)
        {
            try
            {
                if (!IsValidUrl(originalUrl))
                    throw new ArgumentException("Invalid URL format");

                string shortCode;
                Guid? userGuid = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);

                if (!string.IsNullOrEmpty(customAlias))
                {
                    // Ensure alias becomes unique automatically and try to keep something common with customAlias
                    shortCode = await GenerateAvailableAliasAsync(customAlias);
                }
                else
                {
                    // Generate random short code (first deterministic-then-random logic)
                    shortCode = await GenerateUniqueShortCodeAsync(originalUrl);
                }

                var expiresUtc = expiresAt?.ToUniversalTime();

                var urlMapping = new UrlMapping
                {
                    ShortCode = shortCode,
                    OriginalUrl = originalUrl,
                    UserId = userGuid,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresUtc,
                    Title = await ExtractAnyPossibleTitleAsync(originalUrl)
                };

                await _dbContext.UrlMappings.AddAsync(urlMapping);
                await _dbContext.SaveChangesAsync();

                await _cacheService.SetUrlAsync(shortCode, new CachedUrl
                {
                    OriginalUrl = originalUrl,
                    UserId = userId,
                    ExpiresAt = expiresAt
                });

                _logger.LogInformation("Created short URL: {ShortCode} for {OriginalUrl}", shortCode, originalUrl);

                return urlMapping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating short URL for {OriginalUrl}", originalUrl);
                throw;
            }
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

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression =
             System.Net.DecompressionMethods.GZip |
             System.Net.DecompressionMethods.Deflate |
             System.Net.DecompressionMethods.Brotli
        })
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        public async Task<string> ExtractAnyPossibleTitleAsync(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // ✅ Browser-like headers (prevents most 403 blocks)
                request.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120");
                request.Headers.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return GetDomainFallback(url);

                var html = await response.Content.ReadAsStringAsync();

                // ✅ 1. Normal <title>
                var title = Match(html, @"<title>\s*(.*?)\s*</title>");
                if (!string.IsNullOrWhiteSpace(title))
                    return title;

                // ✅ 2. OpenGraph og:title
                title = Match(html, @"property\s*=\s*[""']og:title[""']\s*content\s*=\s*[""'](.*?)[""']");
                if (!string.IsNullOrWhiteSpace(title))
                    return title;

                // ✅ 3. Twitter title
                title = Match(html, @"name\s*=\s*[""']twitter:title[""']\s*content\s*=\s*[""'](.*?)[""']");
                if (!string.IsNullOrWhiteSpace(title))
                    return title;

                // ✅ 4. First H1
                title = Match(html, @"<h1[^>]*>\s*(.*?)\s*</h1>");
                if (!string.IsNullOrWhiteSpace(title))
                    return title;

                // ✅ 5. FINAL FALLBACK → Domain name
                return GetDomainFallback(url);
            }
            catch
            {
                return GetDomainFallback(url); // ✅ GUARANTEED RESULT
            }
        }

        private static string Match(string input, string pattern)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                input,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (!match.Success)
                return null;

            return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
        }

        private static string GetDomainFallback(string url)
        {
            try
            {
                var host = new Uri(url).Host.Replace("www.", "");
                return host.ToUpperInvariant();
            }
            catch
            {
                return "LINK";
            }
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
