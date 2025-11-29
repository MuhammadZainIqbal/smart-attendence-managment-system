using AttendenceManagementSystem.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Represents a single attendance record for a student for a specific class session.
    /// Linked to StudentEnrollment (not directly to Student).
    /// Stores CourseOfferingId for quick queries and reporting.
    /// Stores ClassScheduleId to enforce per-session uniqueness (allows multiple sessions per day).
    /// </summary>
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Foreign Key to StudentEnrollment
        [Required]
        public int StudentEnrollmentId { get; set; }
        public StudentEnrollment StudentEnrollment { get; set; } = null!;

        // Foreign Key to CourseOffering (for quick queries/reporting)
        [Required]
        public int CourseOfferingId { get; set; }
        public CourseOffering CourseOffering { get; set; } = null!;

        // Foreign Key to ClassSchedule (the specific session this attendance is for)
        [Required]
        public int ClassScheduleId { get; set; }
        public ClassSchedule ClassSchedule { get; set; } = null!;

        // Date of attendance (date only, no time component)
        [Required]
        public DateTime Date { get; set; }

        // Attendance status
        [Required]
        public AttendanceStatus Status { get; set; }

        // FK to Teacher who marked this attendance
        [Required]
        public string MarkedByTeacherId { get; set; } = string.Empty;
        public ApplicationUser MarkedByTeacher { get; set; } = null!;

        // Timestamp of when attendance was marked (includes time)
        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Enum for Attendance Status.
    /// Explicit values assigned to prevent data corruption during enum modifications.
    /// </summary>
    public enum AttendanceStatus
    {
        Present = 0,
        Absent = 1,
        Leave = 2
    }
}
