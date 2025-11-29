using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AttendenceManagementSystem.Areas.Identity.Data;
using System.Security.Claims;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Base controller for all Admin-specific controllers.
    /// Provides common functionality like authorization and InstituteId extraction.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public abstract class BaseAdminController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _context;

        /// <summary>
        /// Gets the InstituteId of the currently logged-in Admin.
        /// </summary>
        protected int CurrentInstituteId { get; private set; }

        /// <summary>
        /// Gets the current logged-in Admin user.
        /// </summary>
        protected ApplicationUser? CurrentAdmin { get; private set; }

        public BaseAdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Called before each action executes.
        /// Loads the current admin user and sets the InstituteId.
        /// </summary>
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Get current user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                CurrentAdmin = await _userManager.FindByIdAsync(userId);
                
                if (CurrentAdmin != null)
                {
                    CurrentInstituteId = CurrentAdmin.InstituteId;
                    
                    // Set the DbContext's CurrentInstituteId for Global Query Filters
                    _context.CurrentInstituteId = CurrentInstituteId;
                    
                    // Make InstituteId available in ViewData for views
                    ViewData["CurrentInstituteId"] = CurrentInstituteId;
                    ViewData["CurrentAdminName"] = CurrentAdmin.FullName;
                }
                else
                {
                    // User not found - force logout
                    context.Result = RedirectToAction("Logout", "Account");
                    return;
                }
            }
            else
            {
                // Not authenticated - redirect to login
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            await next();
        }

        /// <summary>
        /// Helper method to verify that an entity belongs to the current admin's institute.
        /// Use this in Edit/Delete actions to prevent IDOR attacks.
        /// </summary>
        protected bool BelongsToCurrentInstitute(int entityInstituteId)
        {
            return entityInstituteId == CurrentInstituteId;
        }

        /// <summary>
        /// Sets success message in TempData.
        /// </summary>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Sets error message in TempData.
        /// </summary>
        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        /// <summary>
        /// Gets the current Institute Time based on the Institute's configured Time Zone.
        /// Converts UTC time to the Institute's local time for time-locked operations.
        /// </summary>
        protected DateTime GetInstituteTime()
        {
            // Get the current institute from the database
            var institute = _context.Institutes.Find(CurrentInstituteId);
            
            if (institute == null || string.IsNullOrEmpty(institute.TimeZoneId))
            {
                // Fallback to Pakistan Standard Time if institute not found
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time")
                );
            }

            // Convert UTC to Institute's local time
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(institute.TimeZoneId)
            );
        }
    }
}
