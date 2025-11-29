using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Student Dashboard and Attendance Report Interface.
    /// Allows students to view their enrolled courses and attendance statistics.
    /// </summary>
    public class StudentController : BaseStudentController
    {
        public StudentController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Student/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            // Get all enrollments for this student
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(se => se.AttendanceRecords)
                .Where(se => se.StudentId == CurrentUserId)
                .ToListAsync();

            var enrolledSubjects = new List<EnrolledSubjectViewModel>();

            foreach (var enrollment in enrollments)
            {
                // Get attendance records for this enrollment
                var attendanceRecords = enrollment.AttendanceRecords.ToList();
                
                var totalClasses = attendanceRecords.Count;
                var presentCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Present);
                var absentCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Absent);
                var leaveCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Leave);
                
                // Calculate percentage
                var percentage = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 0;
                
                // Determine color coding
                string progressBarColor;
                string statusBadge;
                string statusText;
                
                if (percentage >= 80)
                {
                    progressBarColor = "bg-success";
                    statusBadge = "bg-success";
                    statusText = "Excellent";
                }
                else if (percentage >= 70)
                {
                    progressBarColor = "bg-warning";
                    statusBadge = "bg-warning";
                    statusText = "Good";
                }
                else
                {
                    progressBarColor = "bg-danger";
                    statusBadge = "bg-danger";
                    statusText = "Poor";
                }

                enrolledSubjects.Add(new EnrolledSubjectViewModel
                {
                    EnrollmentId = enrollment.Id,
                    SubjectCode = enrollment.CourseOffering.Subject.Code,
                    SubjectName = enrollment.CourseOffering.Subject.Name,
                    TeacherName = enrollment.CourseOffering.Teacher.FullName,
                    BatchName = enrollment.CourseOffering.Batch.Name,
                    SectionName = enrollment.CourseOffering.Section.Name,
                    TotalClasses = totalClasses,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LeaveCount = leaveCount,
                    AttendancePercentage = Math.Round(percentage, 2),
                    ProgressBarColor = progressBarColor,
                    StatusBadge = statusBadge,
                    StatusText = statusText
                });
            }

            // Calculate overall statistics
            var totalSubjects = enrolledSubjects.Count;
            var overallPercentage = enrolledSubjects.Any() 
                ? enrolledSubjects.Average(es => es.AttendancePercentage) 
                : 0;

            var viewModel = new StudentDashboardViewModel
            {
                StudentName = CurrentUser.FullName,
                RollNumber = CurrentUser.RollNumber ?? "N/A",
                BatchName = CurrentUser.Batch?.Name ?? "N/A",
                SectionName = CurrentUser.Section?.Name ?? "N/A",
                EnrolledSubjects = enrolledSubjects,
                TotalSubjects = totalSubjects,
                OverallAttendancePercentage = Math.Round(overallPercentage, 2)
            };

            return View(viewModel);
        }

        // GET: Student/Details/5 (Detailed Report)
        public async Task<IActionResult> Details(int id)
        {
            // Get the enrollment with all related data
            var enrollment = await _context.StudentEnrollments
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(se => se.AttendanceRecords)
                .FirstOrDefaultAsync(se => se.Id == id);

            // Security: Verify enrollment exists and belongs to current student
            if (enrollment == null || enrollment.StudentId != CurrentUserId)
            {
                SetErrorMessage("Enrollment not found or unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            // Get all attendance records with ClassSchedule info (using IgnoreQueryFilters to see archived schedules)
            var attendanceRecords = await _context.AttendanceRecords
                .IgnoreQueryFilters()
                .Where(ar => ar.StudentEnrollmentId == id && ar.InstituteId == CurrentInstituteId)
                .Include(ar => ar.ClassSchedule)
                .OrderByDescending(ar => ar.Date)
                .ToListAsync();

            var totalClasses = attendanceRecords.Count;
            var presentCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Present);
            var absentCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Absent);
            var leaveCount = attendanceRecords.Count(ar => ar.Status == AttendanceStatus.Leave);
            var percentage = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 0;

            // Build attendance history items with "(Archived)" indicator
            var historyItems = attendanceRecords.Select(ar => new AttendanceRecordItem
            {
                Id = ar.Id,
                Date = ar.Date,
                DayOfWeek = ar.Date.DayOfWeek,
                Status = ar.Status,
                MarkedAt = ar.MarkedAt,
                IsArchived = ar.ClassSchedule?.IsDeleted ?? false,
                StatusBadgeClass = ar.Status switch
                {
                    AttendanceStatus.Present => "bg-success",
                    AttendanceStatus.Absent => "bg-danger",
                    AttendanceStatus.Leave => "bg-info",
                    _ => "bg-secondary"
                },
                StatusIcon = ar.Status switch
                {
                    AttendanceStatus.Present => "bi-check-circle-fill",
                    AttendanceStatus.Absent => "bi-x-circle-fill",
                    AttendanceStatus.Leave => "bi-calendar-x-fill",
                    _ => "bi-question-circle-fill"
                }
            }).ToList();

            var viewModel = new StudentReportViewModel
            {
                EnrollmentId = enrollment.Id,
                SubjectCode = enrollment.CourseOffering.Subject.Code,
                SubjectName = enrollment.CourseOffering.Subject.Name,
                TeacherName = enrollment.CourseOffering.Teacher.FullName,
                BatchName = enrollment.CourseOffering.Batch.Name,
                SectionName = enrollment.CourseOffering.Section.Name,
                TotalClasses = totalClasses,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                LeaveCount = leaveCount,
                AttendancePercentage = Math.Round(percentage, 2),
                AttendanceHistory = historyItems
            };

            return View(viewModel);
        }
    }
}
