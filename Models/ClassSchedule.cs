using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Defines the weekly schedule for a CourseOffering (e.g., Mon 10:00-11:00).
    /// Used for the Time-Locked Attendance feature.
    /// </summary>
    public class ClassSchedule
    {
        [Key]
        public int Id { get; set; }

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Foreign Key to CourseOffering
        [Required]
        public int CourseOfferingId { get; set; }
        public CourseOffering CourseOffering { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Grace period in minutes after StartTime during which teacher can mark attendance.
        /// Default: 15 minutes.
        /// </summary>
        [Required]
        public int GracePeriodMinutes { get; set; } = 15;

        /// <summary>
        /// Soft Delete flag. When true, the schedule is hidden from active views
        /// but preserved for attendance history integrity.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
