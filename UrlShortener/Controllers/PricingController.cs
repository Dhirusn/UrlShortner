using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;
using UrlShortener.Data.Models;

namespace UrlShortener.Controllers
{
    public class PricingController : Controller
    {
        private readonly IPricingService _pricingService;
        private readonly ILogger<PricingController> _logger;

        public PricingController(IPricingService pricingService, ILogger<PricingController> logger)
        {
            _pricingService = pricingService;
            _logger = logger;
        }

        // GET: /Pricing
        [HttpGet("/pricing/index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = await _pricingService.GetPricingPageDataAsync();
                return View(viewModel);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading pricing page");
                return View("Error");
            }
        }

        // GET: /Pricing/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _pricingService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        // POST: /Pricing/StartTrial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTrial(PlanSelectionModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return to pricing page with validation errors
                var viewModel = await _pricingService.GetPricingPageDataAsync();
                return View("Index", viewModel);
            }

            try
            {
                var result = await _pricingService.StartTrialAsync(model);

                // Store success message in TempData
                TempData["SuccessMessage"] = result;

                // Redirect to confirmation page
                return RedirectToAction("Confirmation", new { planId = model.PlanId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error starting trial");
                ModelState.AddModelError("", "An error occurred while starting your trial. Please try again.");

                var viewModel = await _pricingService.GetPricingPageDataAsync();
                return View("Index", viewModel);
            }
        }

        // GET: /Pricing/Confirmation
        [HttpGet]
        public async Task<IActionResult> Confirmation(int planId)
        {
            var plan = await _pricingService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.SuccessMessage = TempData["SuccessMessage"] as string;
            return View(plan);
        }

        // AJAX endpoint for toggling billing period
        [HttpPost]
        public IActionResult ToggleBilling([FromBody] BillingToggleRequest request)
        {
            var response = new
            {
                success = true,
                isAnnual = request.IsAnnual,
                message = "Billing period updated"
            };

            return Json(response);
        }

        // API endpoint to get plan details (for AJAX calls)
        [HttpGet]
        public async Task<IActionResult> GetPlan(int id)
        {
            var plan = await _pricingService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            return Json(plan);
        }
    }

    // Helper class for AJAX requests
    public class BillingToggleRequest
    {
        public bool IsAnnual { get; set; }
    }
}