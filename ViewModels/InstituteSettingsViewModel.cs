using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for Institute Settings page.
    /// Allows Admin to configure Time Zone and other institute-level settings.
    /// </summary>
    public class InstituteSettingsViewModel
    {
        // Institute Information (Read-Only Display)
        public string InstituteName { get; set; } = string.Empty;
        public string InstituteCode { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;

        // Time Zone Configuration
        [Required(ErrorMessage = "Please select a Time Zone.")]
        [Display(Name = "Institute Time Zone")]
        public string TimeZoneId { get; set; } = string.Empty;

        // Dropdown List
        public List<SelectListItem> AvailableTimeZones { get; set; } = new List<SelectListItem>();

        // Current Institute Time (for display)
        public DateTime CurrentInstituteTime { get; set; }
    }
}
