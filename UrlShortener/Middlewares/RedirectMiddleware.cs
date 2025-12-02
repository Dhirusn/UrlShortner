using System.Text.RegularExpressions;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Middlewares
{
    public sealed class RedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RedirectMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // ✅ Pre-compiled regex = FAST under heavy traffic
        private static readonly Regex ShortCodeRegex =
            new Regex("^[a-zA-Z0-9_-]{3,10}$", RegexOptions.Compiled);

        public RedirectMiddleware(
            RequestDelegate next,
            ILogger<RedirectMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var rawPath = context.Request.Path.Value;

            if (string.IsNullOrWhiteSpace(rawPath))
            {
                await _next(context);
                return;
            }

            var path = rawPath.Trim('/');

            // ✅ Only intercept VALID short codes
            if (!IsShortCodeRequest(path))
            {
                await _next(context);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var urlService = scope.ServiceProvider.GetRequiredService<IUrlShorteningService>();

            try
            {
                var originalUrl = await urlService.GetOriginalUrlAsync(path);

                if (string.IsNullOrWhiteSpace(originalUrl))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("Short URL not found");
                    return;
                }

                // ✅ Fire click tracking safely in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await urlService.TrackClickAsync(path, context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Click tracking failed for {ShortCode}", path);
                    }
                });

                // ✅ SEO + CDN optimized headers
                context.Response.Headers.CacheControl = "public, max-age=31536000";

                // ✅ 302 during testing, 301 in prod
                var isPermanent = true; // switch via environment if needed
                context.Response.Redirect(originalUrl, permanent: isPermanent);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redirect failed for short code {ShortCode}", path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Redirect service error");
                return;
            }
        }

        private static bool IsShortCodeRequest(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // ✅ Excluded routes (critical for dashboard + auth)
            if (path.StartsWith("api/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("auth/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("dashboard/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("url/", StringComparison.OrdinalIgnoreCase) ||
                path.Contains('.'))
                return false;

            if (path.Length > 12)
                return false;

            return ShortCodeRegex.IsMatch(path);
        }
    }
}
