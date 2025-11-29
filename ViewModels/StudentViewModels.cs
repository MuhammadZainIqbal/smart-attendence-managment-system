using AttendenceManagementSystem.Models;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for Student Dashboard.
    /// Contains list of all enrolled subjects with attendance statistics.
    /// </summary>
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string RollNumber { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        public List<EnrolledSubjectViewModel> EnrolledSubjects { get; set; } = new List<EnrolledSubjectViewModel>();
        
        // Overall statistics
        public int TotalSubjects { get; set; }
        public double OverallAttendancePercentage { get; set; }
    }

    /// <summary>
    /// ViewModel for a single enrolled subject with attendance statistics.
    /// </summary>
    public class EnrolledSubjectViewModel
    {
        public int EnrollmentId { get; set; }
        
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        // Attendance statistics
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        
        public double AttendancePercentage { get; set; }
        
        // UI helpers
        public string ProgressBarColor { get; set; } = "bg-success";
        public string StatusBadge { get; set; } = "badge-success";
        public string StatusText { get; set; } = "Good";
    }

    /// <summary>
    /// ViewModel for detailed attendance report of a single subject.
    /// Shows full history of attendance records.
    /// </summary>
    public class StudentReportViewModel
    {
        public int EnrollmentId { get; set; }
        
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        // Statistics summary
        public int TotalClasses { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public double AttendancePercentage { get; set; }
        
        // Full attendance history
        public List<AttendanceRecordItem> AttendanceHistory { get; set; } = new List<AttendanceRecordItem>();
    }

    /// <summary>
    /// Represents a single attendance record in the history.
    /// </summary>
    public class AttendanceRecordItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime MarkedAt { get; set; }
        
        // Soft delete indicator
        public bool IsArchived { get; set; }
        
        // UI helpers
        public string StatusBadgeClass { get; set; } = string.Empty;
        public string StatusIcon { get; set; } = string.Empty;
    }
}
