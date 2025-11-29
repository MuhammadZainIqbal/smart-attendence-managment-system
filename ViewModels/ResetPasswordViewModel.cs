using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for resetting password after OTP verification.
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
