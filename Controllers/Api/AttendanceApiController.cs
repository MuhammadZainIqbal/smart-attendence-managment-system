using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendenceManagementSystem.Controllers.Api
{
    /// <summary>
    /// JWT-Protected API Controller for Attendance Operations.
    /// Implements Modern Enterprise patterns: JWT Auth, JSON responses, proper error handling.
    /// Used by JavaScript fetch() calls from the Teacher views.
    /// SECURITY: Only Teachers can access these endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Teacher")]
    public class AttendanceApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AttendanceApiController> _logger;
        private readonly IConfiguration _configuration;

        public AttendanceApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AttendanceApiController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
        }

        #region Helper Properties

        /// <summary>
        /// Gets the current user ID from JWT claims.
        /// </summary>
        private string CurrentUserId => User.FindFirstValue("UserId") 
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? string.Empty;

        /// <summary>
        /// Gets the current Institute ID from JWT claims.
        /// </summary>
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

        /// <summary>
        /// Gets current time in Institute's timezone.
        /// </summary>
        private DateTime GetInstituteTime()
        {
            var timeZoneId = _configuration["Institute:TimeZone"] ?? "Pakistan Standard Time";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }

        #endregion

        #region Attendance Marking API

        /// <summary>
        /// POST: api/AttendanceApi/mark
        /// Marks attendance for a class session.
        /// JWT Required: Yes
        /// </summary>
        [HttpPost("mark")]
        [Produces("application/json")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceApiRequest request)
        {
            // 🔐 CRITICAL: Set DbContext InstituteId from JWT for Global Query Filters!
            SetDbContextInstituteId();
            
            _logger.LogInformation("JWT API: MarkAttendance called by User {UserId} for ClassSchedule {ClassScheduleId}, InstituteId {InstituteId}",
                CurrentUserId, request?.ClassScheduleId, CurrentInstituteId);

            // ===== VALIDATION 1: MODEL STATE =====
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Validation failed. Please check your input.",
                    Errors = errors
                });
            }

            // ===== VALIDATION 2: REQUEST NULL CHECK =====
            if (request == null || request.Students == null || !request.Students.Any())
            {
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Invalid request. Student attendance data is required.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Students", new[] { "At least one student attendance record is required." } }
                    }
                });
            }

            // ===== VALIDATION 3: FETCH SCHEDULE =====
            _logger.LogInformation("JWT API: Looking up ClassSchedule with Id={ClassScheduleId}, CurrentUserId={UserId}, CurrentInstituteId={InstituteId}",
                request.ClassScheduleId, CurrentUserId, CurrentInstituteId);
            
            var classSchedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .FirstOrDefaultAsync(cs => cs.Id == request.ClassScheduleId);

            if (classSchedule == null)
            {
                // Debug: Log all available ClassSchedules for this teacher to help diagnose
                var availableSchedules = await _context.ClassSchedules
                    .Include(cs => cs.CourseOffering)
                    .Where(cs => cs.CourseOffering.TeacherId == CurrentUserId)
                    .Select(cs => new { cs.Id, cs.CourseOfferingId, cs.DayOfWeek })
                    .ToListAsync();
                
                _logger.LogWarning("JWT API: ClassSchedule {ClassScheduleId} not found. Available schedules for teacher: {@Schedules}",
                    request.ClassScheduleId, availableSchedules);
                
                return NotFound(new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Class schedule not found. Requested ID: {request.ClassScheduleId}",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "ClassScheduleId", new[] { $"The specified class schedule (ID: {request.ClassScheduleId}) does not exist. Please refresh the page and try again." } }
                    }
                });
            }

            // ===== VALIDATION 4: TEACHER OWNERSHIP (SECURITY) =====
            if (classSchedule.CourseOffering.TeacherId != CurrentUserId)
            {
                _logger.LogWarning("JWT API: Unauthorized attendance attempt. User {UserId} tried to access ClassSchedule {ClassScheduleId}",
                    CurrentUserId, request.ClassScheduleId);

                return StatusCode(403, new ApiErrorResponse
                {
                    Success = false,
                    Message = "Unauthorized. This class schedule does not belong to you.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Authorization", new[] { "You are not authorized to mark attendance for this class." } }
                    }
                });
            }

            // ===== VALIDATION 5: INSTITUTE CHECK (MULTI-TENANT SECURITY) =====
            if (classSchedule.InstituteId != CurrentInstituteId)
            {
                return StatusCode(403, new ApiErrorResponse
                {
                    Success = false,
                    Message = "Multi-tenant security violation.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "InstituteId", new[] { "This schedule belongs to a different institute." } }
                    }
                });
            }

            // ===== VALIDATION 6: DUPLICATE CHECK =====
            var alreadyMarked = await _context.AttendanceRecords
                .AnyAsync(ar => ar.ClassScheduleId == request.ClassScheduleId);

            if (alreadyMarked)
            {
                return Conflict(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Attendance for this session has already been marked.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Duplicate", new[] { "Attendance records already exist for this class session." } }
                    }
                });
            }

            // ===== VALIDATION 7: DAY OF WEEK CHECK =====
            var now = GetInstituteTime();
            var today = now.DayOfWeek;

            if (classSchedule.DayOfWeek != today)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Message = $"This class is scheduled for {classSchedule.DayOfWeek}, not {today}.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "DayOfWeek", new[] { $"You cannot mark attendance for a {classSchedule.DayOfWeek} class on {today}." } }
                    }
                });
            }

            // ===== VALIDATION 8: TIME WINDOW CHECK (CRITICAL SECURITY) =====
            var currentTime = now.TimeOfDay;
            var graceEndTime = classSchedule.StartTime.Add(TimeSpan.FromMinutes(classSchedule.GracePeriodMinutes));

            if (currentTime < classSchedule.StartTime || currentTime > graceEndTime)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Success = false,
                    Message = "The attendance portal is closed.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "TimeWindow", new[] { 
                            $"Attendance can only be marked between {classSchedule.StartTime:hh\\:mm} and {graceEndTime:hh\\:mm}.",
                            $"Current time: {currentTime:hh\\:mm}"
                        } }
                    }
                });
            }

            // ===== ALL VALIDATIONS PASSED - PROCEED WITH SAVE =====
            try
            {
                var utcNow = DateTime.UtcNow;
                var attendanceRecords = new List<AttendanceRecord>();

                foreach (var student in request.Students)
                {
                    // Validate status enum
                    if (!Enum.IsDefined(typeof(AttendanceStatus), student.Status))
                    {
                        return BadRequest(new ApiErrorResponse
                        {
                            Success = false,
                            Message = $"Invalid attendance status for student.",
                            Errors = new Dictionary<string, string[]>
                            {
                                { "Status", new[] { $"Invalid status value: {student.Status}. Must be 0 (Present), 1 (Absent), or 2 (Leave)." } }
                            }
                        });
                    }

                    var record = new AttendanceRecord
                    {
                        InstituteId = CurrentInstituteId,
                        StudentEnrollmentId = student.StudentEnrollmentId,
                        CourseOfferingId = request.CourseOfferingId,
                        ClassScheduleId = request.ClassScheduleId,
                        Date = now.Date,
                        Status = (AttendanceStatus)student.Status,
                        MarkedByTeacherId = CurrentUserId,
                        MarkedAt = utcNow,
                        CreatedAt = utcNow
                    };

                    attendanceRecords.Add(record);
                }

                _context.AttendanceRecords.AddRange(attendanceRecords);
                await _context.SaveChangesAsync();

                // Calculate statistics
                var presentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Present);
                var absentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Absent);
                var leaveCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Leave);

                _logger.LogInformation("JWT API: Attendance marked successfully. ClassSchedule {ClassScheduleId}, Records: {Count}",
                    request.ClassScheduleId, attendanceRecords.Count);

                return Ok(new ApiSuccessResponse
                {
                    Success = true,
                    Message = $"Attendance marked successfully for {attendanceRecords.Count} student(s).",
                    Data = new
                    {
                        TotalStudents = attendanceRecords.Count,
                        Present = presentCount,
                        Absent = absentCount,
                        Leave = leaveCount,
                        MarkedAt = utcNow,
                        ClassScheduleId = request.ClassScheduleId,
                        SubjectCode = classSchedule.CourseOffering.Subject.Code,
                        SubjectName = classSchedule.CourseOffering.Subject.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT API: Error marking attendance for ClassSchedule {ClassScheduleId}", request.ClassScheduleId);

                return StatusCode(500, new ApiErrorResponse
                {
                    Success = false,
                    Message = "An error occurred while saving attendance.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Server", new[] { "Internal server error. Please try again." } }
                    }
                });
            }
        }

        /// <summary>
        /// GET: api/AttendanceApi/schedule/{id}
        /// Gets schedule details and student list for attendance marking.
        /// </summary>
        [HttpGet("schedule/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetScheduleDetails(int id)
        {
            // 🔐 CRITICAL: Set DbContext InstituteId from JWT for Global Query Filters!
            SetDbContextInstituteId();
            
            var classSchedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (classSchedule == null || classSchedule.CourseOffering.TeacherId != CurrentUserId)
            {
                return NotFound(new ApiErrorResponse
                {
                    Success = false,
                    Message = "Schedule not found or unauthorized."
                });
            }

            // Load students
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Where(se => se.CourseOfferingId == classSchedule.CourseOfferingId)
                .OrderBy(se => se.Student.RollNumber)
                .ToListAsync();

            return Ok(new ApiSuccessResponse
            {
                Success = true,
                Data = new
                {
                    ClassScheduleId = classSchedule.Id,
                    CourseOfferingId = classSchedule.CourseOfferingId,
                    SubjectCode = classSchedule.CourseOffering.Subject.Code,
                    SubjectName = classSchedule.CourseOffering.Subject.Name,
                    BatchName = classSchedule.CourseOffering.Batch.Name,
                    SectionName = classSchedule.CourseOffering.Section.Name,
                    StartTime = classSchedule.StartTime.ToString(@"hh\:mm"),
                    EndTime = classSchedule.EndTime.ToString(@"hh\:mm"),
                    Students = enrollments.Select(e => new
                    {
                        StudentEnrollmentId = e.Id,
                        StudentId = e.StudentId,
                        RollNumber = e.Student?.RollNumber ?? "N/A",
                        StudentName = e.Student?.FullName ?? "Unknown"
                    })
                }
            });
        }

        #endregion
    }

    #region API Request/Response Models

    /// <summary>
    /// Request model for marking attendance via API.
    /// </summary>
    public class MarkAttendanceApiRequest
    {
        public int CourseOfferingId { get; set; }
        public int ClassScheduleId { get; set; }
        public List<StudentAttendanceApiModel> Students { get; set; } = new();
    }

    /// <summary>
    /// Individual student attendance in API request.
    /// </summary>
    public class StudentAttendanceApiModel
    {
        public int StudentEnrollmentId { get; set; }
        public int Status { get; set; } // 0 = Present, 1 = Absent, 2 = Leave
    }

    /// <summary>
    /// Standard API success response.
    /// </summary>
    public class ApiSuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Standard API error response with validation errors.
    /// </summary>
    public class ApiErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    #endregion
}
