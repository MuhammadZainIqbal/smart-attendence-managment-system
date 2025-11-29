using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel to display Institute Code after successful registration.
    /// </summary>
    public class SignUpSuccessViewModel
    {
        public string InstituteName { get; set; } = string.Empty;
        public string InstituteCode { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
