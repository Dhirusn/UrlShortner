using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;
using UrlShortener.Models.ViewModels;

namespace UrlShortener.Controllers
{
    public class UrlController : Controller
    {
        private readonly IUrlShorteningService _urlService;
        private readonly ILogger<UrlController> _logger;
        private readonly IConfiguration _configuration;

        public UrlController(IUrlShorteningService urlService, ILogger<UrlController> logger, IConfiguration configuration)
        {
            _urlService = urlService;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: /url/create
        [HttpGet("/url/create")]
        public IActionResult Create()
        {
            return View(new CreateUrlViewModel());
        }

        // POST: /url/create
        [HttpPost("/url/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUrlViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                string userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var mapping = await _urlService.CreateShortUrlAsync(
                    originalUrl: model.OriginalUrl,
                    userId: userId,
                    customAlias: model.CustomAlias,
                    expiresAt: model.ExpiresAt
                );

                // Redirect to details page after creation
                return RedirectToAction(nameof(Details), new { shortCode = mapping.ShortCode });
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(string.Empty, aex.Message);
                return View(model);
            }
            catch (InvalidOperationException iox)
            {
                ModelState.AddModelError(string.Empty, iox.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating short URL");
                ModelState.AddModelError(string.Empty, "Unexpected error occurred");
                return View(model);
            }
        }

        // GET: /url/{shortCode}
        [HttpGet("/url/{shortCode}")]
        public async Task<IActionResult> Details(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
                return NotFound();

            var vm = await _urlService.GetUrlDetailsAsync(shortCode);
            if (vm == null)
                return NotFound();

            return View(vm);
        }

        // GET: /r/{shortCode}  (browser redirect)
        [HttpGet("/r/{shortCode}")]
        public async Task<IActionResult> RedirectToOriginal(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
                return NotFound();

            var original = await _urlService.GetOriginalUrlAsync(shortCode);
            if (string.IsNullOrEmpty(original))
                return NotFound("Short URL not found or expired");

            // Track click asynchronously (non-blocking)
            _ = _urlService.TrackClickAsync(shortCode, HttpContext);

            // Use 302 to preserve analytics; for SEO use 301 if you prefer permanent
            return Redirect(original);
        }

        // GET: /api/url/{shortCode} -> returns original URL (for API consumers)
        [HttpGet("/api/url/{shortCode}")]
        public async Task<IActionResult> GetOriginal([FromRoute] string shortCode)
        {
            var original = await _urlService.GetOriginalUrlAsync(shortCode);
            if (string.IsNullOrEmpty(original))
                return NotFound(new { error = "Short URL not found" });

            return Ok(new { shortCode, originalUrl = original });
        }

        // POST: /api/url -> create short url via API (JSON)
        [HttpPost("/api/url")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateApi([FromBody] CreateUrlRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                string userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
                var mapping = await _urlService.CreateShortUrlAsync(
                    request.OriginalUrl, userId, request.CustomAlias, request.ExpiresAt);

                var shortUrl = $"{GetBaseUrl().TrimEnd('/')}/{mapping.ShortCode}";
                return CreatedAtAction(nameof(GetOriginal), new { shortCode = mapping.ShortCode }, new
                {
                    mapping.ShortCode,
                    shortUrl,
                    mapping.OriginalUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API create error");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Helper: read base url from config or fallback
        private string GetBaseUrl()
        {
            return _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        }
    }

    public class CreateUrlRequest
    {
        public string OriginalUrl { get; set; } = string.Empty;
        public string? CustomAlias { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
