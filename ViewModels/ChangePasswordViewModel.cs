using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for forcing users to change their password on first login.
    /// Used when IsPasswordChanged = false.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm New Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "New Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
