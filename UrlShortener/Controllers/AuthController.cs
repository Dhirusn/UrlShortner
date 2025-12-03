using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Application.Interfaces;
using UrlShortener.Models.ViewModels;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    public class AuthController : Controller
    {
        private readonly IOAuthService _oAuthService;
        private readonly ILogger<AuthController> _logger;
        private readonly IUserService _userService;

        public AuthController(IOAuthService oAuthService, ILogger<AuthController> logger, IUserService userService)
        {
            _oAuthService = oAuthService;
            _logger = logger;
            _userService = userService;
        }

        // GET: /auth/login
        [HttpGet("/auth/login")]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("Login");
        }

        // GET: /auth/signin-external
        [HttpGet("/auth/external/{provider}")]
        public IActionResult ExternalLogin([FromRoute] string provider, string returnUrl = "/")
        {
            if (string.IsNullOrEmpty(provider))
                return BadRequest();

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), "Auth", new { returnUrl }),
                Items =
                {
                    { "scheme", provider }
                }
            };

            // Challenge the external provider
            return Challenge(properties, provider);
        }

        // GET: /auth/callback
        [HttpGet("/auth/callback")]
        public async Task<IActionResult> Callback(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded)
            {
                _logger.LogWarning("External authentication failed.");
                return RedirectToAction(nameof(Login));
            }

            // Process the external principal to create/find app user
            try
            {
                var userId = await _oAuthService.ProcessOAuthUserAsync(result.Principal);
                var appPrincipal = await _oAuthService.CreateApplicationClaimsAsync(userId);

                // Sign in using cookie authentication
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, appPrincipal);

                _logger.LogInformation("User {UserId} signed in.", userId);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OAuth callback.");
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet("/auth/login/local")]
        public IActionResult LoginLocal(string returnUrl = "/")
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("/auth/login/local")]
        public async Task<IActionResult> LoginLocal(LoginViewModel model)
        {
            // If still null, set default
            if (string.IsNullOrEmpty(model.ReturnUrl))
            {
                model.ReturnUrl = "/";
            }
            if (!ModelState.IsValid)
            {
                // Ensure ReturnUrl is preserved when returning the view
                return View("Login", model);
            }

            var user = await _userService.ValidateUserAsync(model.UserNameOrEmail, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid credentials.");
                // Preserve ReturnUrl in the model
                return View("Login", model);
            }

            var principal = _userService.CreatePrincipal(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Use LocalRedirect to prevent open redirect attacks
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        [HttpGet("/auth/register")]
        public IActionResult Register() => View();

        [HttpPost("/auth/register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userService.CreateUserAsync(model.Email, model.UserName, model.Password);

            var principal = _userService.CreatePrincipal(user);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }


        // GET: /auth/logout
        [Authorize]
        [HttpPost("/auth/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // This endpoint intentionally uses the default challenge schemes configured in Program.cs.
        // If you have multiple providers, call /auth/external/{provider} instead.
    }
}
