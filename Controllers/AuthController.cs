using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AttendenceManagementSystem.Areas.Identity.Data;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Custom Authentication Controller for JWT token management.
    /// Provides endpoints for token-related operations.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// POST: /Auth/Logout
        /// Logs out the user and clears JWT cookie.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            // Sign out from Identity (clears authentication cookie)
            await _signInManager.SignOutAsync();
            
            // Clear JWT cookie - MUST use same options as when created
            Response.Cookies.Delete("JWToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,           // DYNAMIC: Must match creation options
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"                          // Must match creation path
            });
            
            _logger.LogInformation("User logged out and JWT cookie cleared.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// GET: /Auth/TokenInfo
        /// Displays JWT token information for the current user.
        /// Useful for debugging/demonstration.
        /// </summary>
        [Authorize]
        public IActionResult TokenInfo()
        {
            var hasToken = Request.Cookies.ContainsKey("JWToken");
            var token = hasToken ? Request.Cookies["JWToken"] : null;
            
            ViewBag.HasToken = hasToken;
            ViewBag.Token = token;
            ViewBag.TokenPreview = !string.IsNullOrEmpty(token) && token.Length > 50 
                ? token.Substring(0, 50) + "..." 
                : token;
            
            return View();
        }
    }
}
