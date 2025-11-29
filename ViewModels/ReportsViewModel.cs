using AttendenceManagementSystem.Models;
using System.Collections.Generic;

namespace AttendenceManagementSystem.ViewModels
{
    /// <summary>
    /// ViewModel for Course Report (Class Analytics).
    /// Shows aggregate stats for an entire course.
    /// </summary>
    public class CourseReportViewModel
    {
        public int CourseOfferingId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        // Aggregates
        public int TotalClassesHeld { get; set; }
        public double ClassAveragePercentage { get; set; }
        
        // Distribution (for Pie/Bar Chart)
        public int GreenCount { get; set; }  // >85%
        public int YellowCount { get; set; } // 70-85%
        public int RedCount { get; set; }    // <70%
        
        // Student Details
        public List<StudentStatsViewModel> StudentStats { get; set; } = new List<StudentStatsViewModel>();
        
        // Chart Data Object (for Chart.js serialization)
        public ChartDataViewModel ChartData { get; set; } = new ChartDataViewModel();
    }

    /// <summary>
    /// Individual student's stats within a course.
    /// Used in the CourseReport table.
    /// </summary>
    public class StudentStatsViewModel
    {
        public int EnrollmentId { get; set; }
        public string RollNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int TotalClasses { get; set; }
        public double AttendancePercentage { get; set; } // (Present / Total) * 100
        
        // Helper for color-coding
        public string StatusBadge
        {
            get
            {
                if (AttendancePercentage >= 85) return "success"; // Green
                if (AttendancePercentage >= 70) return "warning"; // Yellow
                return "danger"; // Red
            }
        }
    }

    /// <summary>
    /// ViewModel for Individual Student Report (Deep Dive).
    /// Shows date-wise attendance history for a specific enrollment.
    /// </summary>
    public class StudentDetailReportViewModel
    {
        public int EnrollmentId { get; set; }
        public int CourseOfferingId { get; set; } // For "Back to Course" navigation
        public string StudentName { get; set; } = string.Empty;
        public string RollNumber { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        
        // Aggregates
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int TotalClasses { get; set; }
        public double AttendancePercentage { get; set; } // (Present / Total) * 100
        
        // Detailed History
        public List<AttendanceRecordViewModel> AttendanceHistory { get; set; } = new List<AttendanceRecordViewModel>();
        
        // Chart Data Object
        public ChartDataViewModel ChartData { get; set; } = new ChartDataViewModel();
    }

    /// <summary>
    /// Simplified Attendance Record for display.
    /// </summary>
    public class AttendanceRecordViewModel
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public AttendanceStatus Status { get; set; }
        public string TimeSlot { get; set; } = string.Empty; // e.g., "09:00 - 11:00"
        public DateTime MarkedAt { get; set; }
        
        // Helper for Bootstrap badge color
        public string StatusBadge
        {
            get
            {
                return Status switch
                {
                    AttendanceStatus.Present => "success",
                    AttendanceStatus.Absent => "danger",
                    AttendanceStatus.Leave => "warning",
                    _ => "secondary"
                };
            }
        }
    }

    /// <summary>
    /// Chart data structure for Chart.js serialization.
    /// </summary>
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<int> Data { get; set; } = new List<int>();
        public List<string> BackgroundColors { get; set; } = new List<string>();
    }

    /// <summary>
    /// ViewModel for Reports Index (Landing Page).
    /// Shows list of courses taught by the teacher.
    /// </summary>
    public class ReportsIndexViewModel
    {
        public List<TeacherCourseViewModel> Courses { get; set; } = new List<TeacherCourseViewModel>();
    }

    /// <summary>
    /// Simplified Course Info for Reports Index.
    /// </summary>
    public class TeacherCourseViewModel
    {
        public int CourseOfferingId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
    }
}
