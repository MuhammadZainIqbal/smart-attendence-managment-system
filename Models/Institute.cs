using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Represents a Tenant in the Multi-Tenant system.
    /// Each Institute is an independent educational organization.
    /// </summary>
    public class Institute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique Institute Code (e.g., "PU-9021").
        /// Auto-generated on creation. Used during login to identify tenant.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string AdminEmail { get; set; } = string.Empty;

        /// <summary>
        /// Time Zone ID for the Institute (Windows format, e.g., "Pakistan Standard Time").
        /// Used to convert UTC time to local institute time for time-locked operations.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TimeZoneId { get; set; } = "Pakistan Standard Time";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Areas.Identity.Data.ApplicationUser> Users { get; set; } = new List<Areas.Identity.Data.ApplicationUser>();
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
        public ICollection<Section> Sections { get; set; } = new List<Section>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
        public ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
        public ICollection<StudentEnrollment> StudentEnrollments { get; set; } = new List<StudentEnrollment>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
