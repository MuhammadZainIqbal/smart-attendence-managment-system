using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for Multi-Tenant Login.
    /// Requires Email, Password, and Institute Code.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Institute Code is required")]
        [StringLength(20, ErrorMessage = "Institute Code is invalid")]
        [Display(Name = "Institute Code")]
        public string InstituteCode { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
