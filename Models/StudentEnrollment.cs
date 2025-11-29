using AttendenceManagementSystem.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Links a Student to a CourseOffering.
    /// Supports both bulk enrollment (entire section) and individual repeater enrollment.
    /// Explicit records are created for all enrollments.
    /// </summary>
    public class StudentEnrollment
    {
        [Key]
        public int Id { get; set; }

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Foreign Keys
        [Required]
        public string StudentId { get; set; } = string.Empty; // FK to ApplicationUser (Student role)
        public ApplicationUser Student { get; set; } = null!;

        [Required]
        public int CourseOfferingId { get; set; }
        public CourseOffering CourseOffering { get; set; } = null!;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
