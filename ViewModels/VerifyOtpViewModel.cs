using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for OTP verification (both Sign Up and Password Reset flows).
    /// </summary>
    public class VerifyOtpViewModel
    {
        // Optional: Used for Sign Up flow only
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be a 6-digit number")]
        [Display(Name = "6-Digit OTP Code")]
        public string Code { get; set; } = string.Empty;

        // Optional: Used for Password Reset flow (required there)
        public string Email { get; set; } = string.Empty;
    }
}
