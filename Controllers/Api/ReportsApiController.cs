using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendenceManagementSystem.Controllers.Api
{
    /// <summary>
    /// JWT-Protected API Controller for Reports Operations.
    /// Implements Modern Enterprise patterns: JWT Auth, JSON responses, dynamic filtering.
    /// Used by JavaScript fetch() calls from the Reports views.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Teacher")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReportsApiController> _logger;

        public ReportsApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ReportsApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        #region Helper Properties

        private string CurrentUserId => User.FindFirstValue("UserId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        private int CurrentInstituteId
        {
            get
            {
                var claim = User.FindFirstValue("InstituteId");
                return int.TryParse(claim, out int id) ? id : 0;
            }
        }

        /// <summary>
        /// CRITICAL: Sets the DbContext's CurrentInstituteId from JWT claims.
        /// This is required for the Global Query Filters to work properly.
        /// Must be called before ANY database query to ensure multi-tenant isolation.
        /// </summary>
        private void SetDbContextInstituteId()
        {
            var instituteId = CurrentInstituteId;
            if (instituteId > 0)
            {
                _context.CurrentInstituteId = instituteId;
                _logger.LogDebug("DbContext InstituteId set to {InstituteId} from JWT claims", instituteId);
            }
            else
            {
                _logger.LogWarning("JWT claims did not contain a valid InstituteId!");
            }
        }

        #endregion

        #region Course Report API

        /// <summary>
        /// GET: api/ReportsApi/course/{id}
        /// Gets course report data with optional date filtering.
        /// Returns data for dynamic table update via AJAX.
        /// </summary>
        [HttpGet("course/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetCourseReport(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // 🔐 CRITICAL: Set DbContext InstituteId from JWT for Global Query Filters!
            SetDbContextInstituteId();
            
            _logger.LogInformation("JWT API: GetCourseReport called by User {UserId} for Course {CourseId}, DateRange: {StartDate} to {EndDate}",
                CurrentUserId, id, startDate, endDate);

            // Fetch CourseOffering
            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (courseOffering == null)
            {
                return NotFound(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Course offering not found."
                });
            }

            // Security: Verify teacher ownership
            if (courseOffering.TeacherId != CurrentUserId)
            {
                return StatusCode(403, new ApiErrorResponse
                {
                    Success = false,
                    Message = "Unauthorized. This course does not belong to you."
                });
            }

            // Default date range: Last 30 days
            var effectiveEndDate = endDate ?? DateTime.Today;
            var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

            // Validate date range
            if (effectiveStartDate > effectiveEndDate)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Invalid date range.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "DateRange", new[] { "Start date cannot be after end date." } }
                    }
                });
            }

            // Calculate Total Classes Held
            var totalClassesHeld = await _context.AttendanceRecords
                .Where(ar => ar.CourseOfferingId == id)
                .Where(ar => ar.Date >= effectiveStartDate && ar.Date <= effectiveEndDate)
                .Select(ar => new { ar.ClassScheduleId, ar.Date })
                .Distinct()
                .CountAsync();

            if (totalClassesHeld == 0)
            {
                return Ok(new ApiSuccessResponse
                {
                    Success = true,
                    Message = "No attendance records found for the selected date range.",
                    Data = new
                    {
                        CourseInfo = new
                        {
                            CourseOfferingId = courseOffering.Id,
                            SubjectCode = courseOffering.Subject.Code,
                            SubjectName = courseOffering.Subject.Name,
                            BatchName = courseOffering.Batch.Name,
                            SectionName = courseOffering.Section.Name
                        },
                        DateRange = new
                        {
                            StartDate = effectiveStartDate.ToString("yyyy-MM-dd"),
                            EndDate = effectiveEndDate.ToString("yyyy-MM-dd")
                        },
                        Summary = new
                        {
                            TotalClassesHeld = 0,
                            TotalStudents = 0,
                            ClassAveragePercentage = 0.0,
                            GreenCount = 0,
                            YellowCount = 0,
                            RedCount = 0
                        },
                        Students = new List<object>(),
                        ChartData = new
                        {
                            Labels = new[] { "Excellent (≥85%)", "Good (70-84%)", "Poor (<70%)" },
                            Data = new[] { 0, 0, 0 },
                            BackgroundColors = new[] { "#28a745", "#ffc107", "#dc3545" }
                        }
                    }
                });
            }

            // Fetch enrollments
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Where(se => se.CourseOfferingId == id)
                .OrderBy(se => se.Student.RollNumber)
                .ToListAsync();

            var studentStats = new List<object>();
            int greenCount = 0, yellowCount = 0, redCount = 0;
            double totalPercentage = 0;

            foreach (var enrollment in enrollments)
            {
                var records = await _context.AttendanceRecords
                    .Where(ar => ar.StudentEnrollmentId == enrollment.Id)
                    .Where(ar => ar.Date >= effectiveStartDate && ar.Date <= effectiveEndDate)
                    .ToListAsync();

                int presentCount = records.Count(r => r.Status == AttendanceStatus.Present);
                int absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
                int leaveCount = records.Count(r => r.Status == AttendanceStatus.Leave);

                double attendancePercentage = totalClassesHeld > 0
                    ? (double)presentCount / totalClassesHeld * 100
                    : 0;

                totalPercentage += attendancePercentage;

                // Categorize
                if (attendancePercentage >= 85) greenCount++;
                else if (attendancePercentage >= 70) yellowCount++;
                else redCount++;

                // Determine status class for frontend styling
                string statusClass = attendancePercentage >= 85 ? "success"
                    : attendancePercentage >= 70 ? "warning"
                    : "danger";

                studentStats.Add(new
                {
                    EnrollmentId = enrollment.Id,
                    RollNumber = enrollment.Student?.RollNumber ?? "N/A",
                    StudentName = enrollment.Student?.FullName ?? "Unknown",
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LeaveCount = leaveCount,
                    TotalClasses = totalClassesHeld,
                    AttendancePercentage = Math.Round(attendancePercentage, 2),
                    StatusClass = statusClass
                });
            }

            double classAverage = enrollments.Count > 0 ? totalPercentage / enrollments.Count : 0;

            return Ok(new ApiSuccessResponse
            {
                Success = true,
                Message = "Report data retrieved successfully.",
                Data = new
                {
                    CourseInfo = new
                    {
                        CourseOfferingId = courseOffering.Id,
                        SubjectCode = courseOffering.Subject.Code,
                        SubjectName = courseOffering.Subject.Name,
                        BatchName = courseOffering.Batch.Name,
                        SectionName = courseOffering.Section.Name
                    },
                    DateRange = new
                    {
                        StartDate = effectiveStartDate.ToString("yyyy-MM-dd"),
                        EndDate = effectiveEndDate.ToString("yyyy-MM-dd"),
                        StartDateFormatted = effectiveStartDate.ToString("dd MMM yyyy"),
                        EndDateFormatted = effectiveEndDate.ToString("dd MMM yyyy")
                    },
                    Summary = new
                    {
                        TotalClassesHeld = totalClassesHeld,
                        TotalStudents = enrollments.Count,
                        ClassAveragePercentage = Math.Round(classAverage, 2),
                        GreenCount = greenCount,
                        YellowCount = yellowCount,
                        RedCount = redCount
                    },
                    Students = studentStats,
                    ChartData = new
                    {
                        Labels = new[] { "Excellent (≥85%)", "Good (70-84%)", "Poor (<70%)" },
                        Data = new[] { greenCount, yellowCount, redCount },
                        BackgroundColors = new[] { "#28a745", "#ffc107", "#dc3545" }
                    }
                }
            });
        }

        /// <summary>
        /// GET: api/ReportsApi/student/{id}
        /// Gets individual student attendance details with date filtering.
        /// </summary>
        [HttpGet("student/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetStudentReport(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // 🔐 CRITICAL: Set DbContext InstituteId from JWT for Global Query Filters!
            SetDbContextInstituteId();
            
            _logger.LogInformation("JWT API: GetStudentReport called by User {UserId} for Enrollment {EnrollmentId}",
                CurrentUserId, id);

            // Fetch enrollment
            var enrollment = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Section)
                .FirstOrDefaultAsync(se => se.Id == id);

            if (enrollment == null || enrollment.CourseOffering.TeacherId != CurrentUserId)
            {
                return NotFound(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Enrollment not found or unauthorized."
                });
            }

            // Default date range
            var effectiveEndDate = endDate ?? DateTime.Today;
            var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

            // Fetch attendance records
            var records = await _context.AttendanceRecords
                .Include(ar => ar.ClassSchedule)
                .Where(ar => ar.StudentEnrollmentId == id)
                .Where(ar => ar.Date >= effectiveStartDate && ar.Date <= effectiveEndDate)
                .OrderByDescending(ar => ar.Date)
                .ToListAsync();

            int presentCount = records.Count(r => r.Status == AttendanceStatus.Present);
            int absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
            int leaveCount = records.Count(r => r.Status == AttendanceStatus.Leave);
            int totalClasses = records.Count;

            double attendancePercentage = totalClasses > 0
                ? (double)presentCount / totalClasses * 100
                : 0;

            return Ok(new ApiSuccessResponse
            {
                Success = true,
                Message = "Student report retrieved successfully.",
                Data = new
                {
                    StudentInfo = new
                    {
                        EnrollmentId = enrollment.Id,
                        RollNumber = enrollment.Student?.RollNumber ?? "N/A",
                        StudentName = enrollment.Student?.FullName ?? "Unknown",
                        Email = enrollment.Student?.Email ?? "N/A"
                    },
                    CourseInfo = new
                    {
                        SubjectCode = enrollment.CourseOffering.Subject.Code,
                        SubjectName = enrollment.CourseOffering.Subject.Name,
                        BatchName = enrollment.CourseOffering.Batch.Name,
                        SectionName = enrollment.CourseOffering.Section.Name
                    },
                    DateRange = new
                    {
                        StartDate = effectiveStartDate.ToString("yyyy-MM-dd"),
                        EndDate = effectiveEndDate.ToString("yyyy-MM-dd")
                    },
                    Summary = new
                    {
                        TotalClasses = totalClasses,
                        PresentCount = presentCount,
                        AbsentCount = absentCount,
                        LeaveCount = leaveCount,
                        AttendancePercentage = Math.Round(attendancePercentage, 2)
                    },
                    Records = records.Select(r => new
                    {
                        Date = r.Date.ToString("yyyy-MM-dd"),
                        DateFormatted = r.Date.ToString("dd MMM yyyy"),
                        DayOfWeek = r.Date.DayOfWeek.ToString(),
                        Status = r.Status.ToString(),
                        StatusValue = (int)r.Status,
                        Time = r.ClassSchedule != null
                            ? $"{r.ClassSchedule.StartTime:hh\\:mm} - {r.ClassSchedule.EndTime:hh\\:mm}"
                            : "N/A"
                    })
                }
            });
        }

        /// <summary>
        /// GET: api/ReportsApi/courses
        /// Gets list of courses assigned to the current teacher.
        /// </summary>
        [HttpGet("courses")]
        [Produces("application/json")]
        public async Task<IActionResult> GetTeacherCourses()
        {
            // 🔐 CRITICAL: Set DbContext InstituteId from JWT for Global Query Filters!
            SetDbContextInstituteId();
            
            var courses = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Where(co => co.TeacherId == CurrentUserId)
                .OrderBy(co => co.Subject.Code)
                .ToListAsync();

            var result = new List<object>();

            foreach (var course in courses)
            {
                var studentCount = await _context.StudentEnrollments
                    .CountAsync(se => se.CourseOfferingId == course.Id);

                result.Add(new
                {
                    CourseOfferingId = course.Id,
                    SubjectCode = course.Subject.Code,
                    SubjectName = course.Subject.Name,
                    BatchName = course.Batch.Name,
                    SectionName = course.Section.Name,
                    TotalStudents = studentCount
                });
            }

            return Ok(new ApiSuccessResponse
            {
                Success = true,
                Data = result
            });
        }

        #endregion
    }
}
