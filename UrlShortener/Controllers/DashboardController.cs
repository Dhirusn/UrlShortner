using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;
using UrlShortener.Models.ViewModels;

namespace UrlShortener.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUrlShorteningService _urlService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IUrlShorteningService urlService, ILogger<DashboardController> logger)
        {
            _urlService = urlService;
            _logger = logger;
        }

        // GET: /dashboard
        [HttpGet("/dashboard/Index")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Challenge(); // Shouldn't happen due to [Authorize]

            try
            {
                var (items, total) = await _urlService.GetUserUrlsAsync(userId, page, pageSize);
                var vm = new DashboardViewModel
                {
                    Urls = items,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                };

                return View("Index", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard for user {UserId}", userId);
                return StatusCode(500, "Failed to load dashboard");
            }
        }

        // GET: /dashboard/analytics/{shortCode}
        [HttpGet("/dashboard/analytics/{shortCode}")]
        public async Task<IActionResult> Analytics(string shortCode)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var details = await _urlService.GetUrlDetailsAsync(shortCode);
            if (details == null)
                return NotFound();

            // Optional: validate ownership if UrlMapping includes UserId
            // If details show ownership info you can check and return 403 if not owner.

            return View("Analytics", details);
        }

        // POST: /dashboard/delete/{shortCode}
        [HttpPost("/dashboard/delete/{shortCode}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string shortCode)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            try
            {
                var success = await _urlService.DeleteUrlAsync(shortCode, userId);
                if (!success)
                    return BadRequest("Unable to delete URL");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting short URL {ShortCode} for user {UserId}", shortCode, userId);
                return StatusCode(500, "Failed to delete URL");
            }
        }
    }
}
