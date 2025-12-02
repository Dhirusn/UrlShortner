using Microsoft.AspNetCore.Mvc;

namespace UrlShortener.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // GET: /
        public IActionResult Index()
        {
            ViewData["AppName"] = _configuration["AppName"] ?? "UrlShortener";
            return View();
        }

        // GET: /privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: /health
        [HttpGet("/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "Healthy", time = DateTime.UtcNow });
        }

        // GET: /version
        [HttpGet("/version")]
        public IActionResult Version()
        {
            var version = typeof(HomeController).Assembly.GetName().Version?.ToString() ?? "unknown";
            return Ok(new { name = "UrlShortener", version });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Implement your Error view if you have one
            return View("Error");
        }
    }
}
