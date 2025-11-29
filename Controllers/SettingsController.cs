using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Admin Settings Controller.
    /// Allows Institute Admins to configure system-wide settings like Time Zone.
    /// </summary>
    public class SettingsController : BaseAdminController
    {
        public SettingsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Settings/Edit
        public async Task<IActionResult> Edit()
        {
            // Get the current institute
            var institute = await _context.Institutes
                .FirstOrDefaultAsync(i => i.Id == CurrentInstituteId);

            if (institute == null)
            {
                SetErrorMessage("Institute not found.");
                return RedirectToAction("Index", "Admin");
            }

            // Populate Time Zone dropdown
            var timeZones = TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => new SelectListItem
                {
                    Value = tz.Id,
                    Text = $"{tz.DisplayName}",
                    Selected = tz.Id == institute.TimeZoneId
                })
                .ToList();

            var viewModel = new InstituteSettingsViewModel
            {
                InstituteName = institute.Name,
                InstituteCode = institute.Code,
                AdminEmail = institute.AdminEmail,
                TimeZoneId = institute.TimeZoneId,
                AvailableTimeZones = timeZones,
                CurrentInstituteTime = GetInstituteTime()
            };

            return View(viewModel);
        }

        // POST: Settings/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstituteSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdown on error
                model.AvailableTimeZones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Value = tz.Id,
                        Text = $"{tz.DisplayName}",
                        Selected = tz.Id == model.TimeZoneId
                    })
                    .ToList();

                return View(model);
            }

            // Get the current institute
            var institute = await _context.Institutes
                .FirstOrDefaultAsync(i => i.Id == CurrentInstituteId);

            if (institute == null)
            {
                SetErrorMessage("Institute not found.");
                return RedirectToAction("Index", "Admin");
            }

            // Validate Time Zone ID
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(model.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                ModelState.AddModelError("TimeZoneId", "Invalid Time Zone selected.");
                
                // Repopulate dropdown
                model.AvailableTimeZones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Value = tz.Id,
                        Text = $"{tz.DisplayName}",
                        Selected = tz.Id == model.TimeZoneId
                    })
                    .ToList();

                return View(model);
            }

            // Update the institute
            institute.TimeZoneId = model.TimeZoneId;

            try
            {
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Time Zone updated successfully to: {model.TimeZoneId}");
                return RedirectToAction(nameof(Edit));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error updating settings: {ex.Message}");
                
                // Repopulate dropdown
                model.AvailableTimeZones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new SelectListItem
                    {
                        Value = tz.Id,
                        Text = $"{tz.DisplayName}",
                        Selected = tz.Id == model.TimeZoneId
                    })
                    .ToList();

                return View(model);
            }
        }
    }
}
