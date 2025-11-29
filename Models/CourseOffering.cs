using AttendenceManagementSystem.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Represents a "Class" - a Teacher assigned to teach a Subject 
    /// for a specific Section and Batch.
    /// This is the core entity linking Teacher, Subject, Section, and Batch.
    /// </summary>
    public class CourseOffering
    {
        [Key]
        public int Id { get; set; }

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Foreign Keys
        [Required]
        public string TeacherId { get; set; } = string.Empty; // FK to ApplicationUser (Teacher role)
        public ApplicationUser Teacher { get; set; } = null!;

        [Required]
        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        [Required]
        public int SectionId { get; set; }
        public Section Section { get; set; } = null!;

        [Required]
        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public ICollection<StudentEnrollment> StudentEnrollments { get; set; } = new List<StudentEnrollment>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
