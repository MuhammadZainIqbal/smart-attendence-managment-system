using AttendenceManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for marking attendance for a specific class session.
    /// Contains the course details, session details, and list of students to mark attendance for.
    /// </summary>
    public class MarkAttendanceViewModel
    {
        public int CourseOfferingId { get; set; }
        public int ClassScheduleId { get; set; }
        
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        public DateTime Date { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        public List<StudentAttendanceInputModel> Students { get; set; } = new List<StudentAttendanceInputModel>();
    }

    /// <summary>
    /// Represents a single student's attendance input in the form.
    /// </summary>
    public class StudentAttendanceInputModel
    {
        public int StudentEnrollmentId { get; set; }
        
        public string StudentId { get; set; } = string.Empty;
        public string RollNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        
        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    }

    /// <summary>
    /// ViewModel for the "Attendance Portal Closed" error page.
    /// Shows why attendance marking is not allowed at the current time.
    /// </summary>
    public class AttendancePortalClosedViewModel
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        public DayOfWeek ScheduledDay { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GracePeriodMinutes { get; set; }
        
        public DateTime CurrentTime { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for Teacher Dashboard with Smart Banner status.
    /// Shows current class status (Active, Upcoming, or None).
    /// </summary>
    public class TeacherDashboardViewModel
    {
        public List<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
        public CurrentClassStatus ClassStatus { get; set; } = new CurrentClassStatus();
    }

    /// <summary>
    /// Represents the current status of classes for the Smart Banner.
    /// </summary>
    public class CurrentClassStatus
    {
        public ClassStatusType Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CssClass { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        
        // For Active status
        public int? ClassScheduleId { get; set; }
        public int? CourseOfferingId { get; set; }
        public string? SubjectCode { get; set; }
        public string? SubjectName { get; set; }
        public string? BatchName { get; set; }
        public string? SectionName { get; set; }
        public int? MinutesRemaining { get; set; }
        
        // For Live Countdown (Server-synced)
        public int? SecondsRemaining { get; set; }
        
        // For Upcoming status
        public TimeSpan? UpcomingStartTime { get; set; }
        public int? SecondsUntilStart { get; set; }
    }

    /// <summary>
    /// Enum for class status types.
    /// </summary>
    public enum ClassStatusType
    {
        Active,      // Class is live now, within grace period
        Upcoming,    // Class is scheduled later today
        None         // No classes today or all finished
    }
}
