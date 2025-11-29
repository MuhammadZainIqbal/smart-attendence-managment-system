using AttendenceManagementSystem.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Base controller for all Teacher-specific controllers.
    /// Provides common functionality like multi-tenancy support and user context.
    /// Enforces Teacher role authorization.
    /// </summary>
    [Authorize(Roles = "Teacher")]
    public abstract class BaseTeacherController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly ApplicationDbContext _context;

        /// <summary>
        /// The InstituteId of the current logged-in Teacher.
        /// Available after OnActionExecutionAsync runs.
        /// </summary>
        protected int CurrentInstituteId { get; private set; }

        /// <summary>
        /// The UserId of the current logged-in Teacher.
        /// Available after OnActionExecutionAsync runs.
        /// </summary>
        protected string CurrentUserId { get; private set; } = string.Empty;

        /// <summary>
        /// The current logged-in Teacher's ApplicationUser object.
        /// Available after OnActionExecutionAsync runs.
        /// </summary>
        protected ApplicationUser CurrentUser { get; private set; } = null!;

        public BaseTeacherController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Executes before every action.
        /// Sets CurrentInstituteId, CurrentUserId, and CurrentUser for the logged-in teacher.
        /// Also propagates CurrentInstituteId to the DbContext for Global Query Filters.
        /// </summary>
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Should not happen due to [Authorize], but safety check
                context.Result = RedirectToAction("Login", "Account", new { area = "Identity" });
                return;
            }

            // Set properties
            CurrentInstituteId = user.InstituteId;
            CurrentUserId = user.Id;
            CurrentUser = user;

            // Propagate to DbContext for Global Query Filters
            _context.CurrentInstituteId = CurrentInstituteId;

            // Continue to action
            await base.OnActionExecutionAsync(context, next);
        }

        /// <summary>
        /// Verifies if an entity belongs to the current teacher's institute.
        /// Use this for security checks before performing operations.
        /// </summary>
        protected bool BelongsToCurrentInstitute(int entityInstituteId)
        {
            return entityInstituteId == CurrentInstituteId;
        }

        /// <summary>
        /// Sets a success message in TempData to be displayed after redirect.
        /// </summary>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Sets an error message in TempData to be displayed after redirect.
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
