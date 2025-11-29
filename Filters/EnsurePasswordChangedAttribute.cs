using AttendenceManagementSystem.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AttendenceManagementSystem.Filters
{
    /// <summary>
    /// Action filter that enforces password change for users created by Admin.
    /// Redirects to /Account/ChangePassword if IsPasswordChanged is false.
    /// Prevents redirect loops by excluding AccountController.
    /// </summary>
    public class EnsurePasswordChangedAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Check if user is authenticated
            if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                // Get the controller name
                var controllerName = context.RouteData.Values["controller"]?.ToString();
                var actionName = context.RouteData.Values["action"]?.ToString();

                // Exclude AccountController to prevent redirect loops
                // Allow: Login, Logout, ChangePassword, VerifyEmail, etc.
                if (controllerName?.Equals("Account", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await next();
                    return;
                }

                // Get UserManager from services
                var userManager = context.HttpContext.RequestServices
                    .GetService<UserManager<ApplicationUser>>();

                if (userManager != null)
                {
                    var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await userManager.FindByIdAsync(userId);

                        // If user exists and has NOT changed their password
                        if (user != null && !user.IsPasswordChanged)
                        {
                            // Redirect to ChangePassword
                            context.Result = new RedirectToActionResult(
                                "ChangePassword",
                                "Account",
                                null
                            );
                            return;
                        }
                    }
                }
            }

            await next();
        }
    }
}
