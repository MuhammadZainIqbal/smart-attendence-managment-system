using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Teacher Dashboard and Attendance Marking Interface.
    /// Allows teachers to view their assigned courses and mark attendance within valid time slots.
    /// </summary>
    public class TeacherController : BaseTeacherController
    {
        public TeacherController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Teacher/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            // Get all course offerings assigned to this teacher
            var courseOfferings = await _context.CourseOfferings
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Include(co => co.Subject)
                .Where(co => co.TeacherId == CurrentUserId)
                .OrderBy(co => co.Subject.Code)
                .ThenBy(co => co.Batch.Name)
                .ThenBy(co => co.Section.Name)
                .ToListAsync();

            // ===== SMART BANNER LOGIC =====
            var now = GetInstituteTime();
            var today = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            var classStatus = new CurrentClassStatus
            {
                Status = ClassStatusType.None,
                Message = "No classes scheduled for today.",
                CssClass = "alert-secondary",
                IconClass = "bi-calendar-x"
            };

            // Get all course offering IDs for this teacher
            var courseOfferingIds = courseOfferings.Select(co => co.Id).ToList();

            if (courseOfferingIds.Any())
            {
                // Get all class schedules for today
                var todaySchedules = await _context.ClassSchedules
                    .Include(cs => cs.CourseOffering)
                        .ThenInclude(co => co.Subject)
                    .Include(cs => cs.CourseOffering)
                        .ThenInclude(co => co.Batch)
                    .Include(cs => cs.CourseOffering)
                        .ThenInclude(co => co.Section)
                    .Where(cs => courseOfferingIds.Contains(cs.CourseOfferingId) && cs.DayOfWeek == today)
                    .OrderBy(cs => cs.StartTime)
                    .ToListAsync();

                if (todaySchedules.Any())
                {
                    // Check for ACTIVE class (within grace period)
                    ClassSchedule? activeSchedule = null;
                    foreach (var schedule in todaySchedules)
                    {
                        var graceEndTime = schedule.StartTime.Add(TimeSpan.FromMinutes(schedule.GracePeriodMinutes));
                        
                        if (currentTime >= schedule.StartTime && currentTime <= graceEndTime)
                        {
                            // Check if attendance already marked for THIS SPECIFIC SESSION
                            var attendanceCount = await _context.AttendanceRecords
                                .Where(ar => ar.ClassScheduleId == schedule.Id)
                                .CountAsync();
                            
                            System.Diagnostics.Debug.WriteLine($"[TeacherDashboard] ClassSchedule {schedule.Id}: Found {attendanceCount} attendance records for this session");
                            
                            var alreadyMarked = attendanceCount > 0;
                            
                            if (!alreadyMarked)
                            {
                                activeSchedule = schedule;
                                break;
                            }
                        }
                    }

                    if (activeSchedule != null)
                    {
                        // ACTIVE CLASS FOUND
                        var graceEndTime = activeSchedule.StartTime.Add(TimeSpan.FromMinutes(activeSchedule.GracePeriodMinutes));
                        var minutesRemaining = (int)(graceEndTime - currentTime).TotalMinutes;
                        var secondsRemaining = (int)(graceEndTime - currentTime).TotalSeconds;

                        classStatus = new CurrentClassStatus
                        {
                            Status = ClassStatusType.Active,
                            Message = $"🟢 LIVE NOW: {activeSchedule.CourseOffering.Subject.Code} - {activeSchedule.CourseOffering.Section.Name}. Portal closes soon!",
                            CssClass = "alert-success",
                            IconClass = "bi-broadcast",
                            ClassScheduleId = activeSchedule.Id,
                            CourseOfferingId = activeSchedule.CourseOfferingId,
                            SubjectCode = activeSchedule.CourseOffering.Subject.Code,
                            SubjectName = activeSchedule.CourseOffering.Subject.Name,
                            BatchName = activeSchedule.CourseOffering.Batch.Name,
                            SectionName = activeSchedule.CourseOffering.Section.Name,
                            MinutesRemaining = minutesRemaining,
                            SecondsRemaining = secondsRemaining
                        };
                    }
                    else
                    {
                        // Check for UPCOMING class (classes that haven't finished yet)
                        ClassSchedule? upcomingSchedule = null;
                        
                        foreach (var schedule in todaySchedules)
                        {
                            var graceEndTime = schedule.StartTime.Add(TimeSpan.FromMinutes(schedule.GracePeriodMinutes));
                            
                            // Consider a class "upcoming" if:
                            // 1. It hasn't started yet (StartTime > currentTime), OR
                            // 2. It's currently active but attendance was already marked
                            if (currentTime < graceEndTime)
                            {
                                upcomingSchedule = schedule;
                                break;
                            }
                        }

                        if (upcomingSchedule != null)
                        {
                            // Determine if class has started or not
                            if (currentTime < upcomingSchedule.StartTime)
                            {
                                // Class hasn't started yet - calculate seconds until start
                                var secondsUntilStart = (int)(upcomingSchedule.StartTime - currentTime).TotalSeconds;
                                
                                classStatus = new CurrentClassStatus
                                {
                                    Status = ClassStatusType.Upcoming,
                                    Message = $"⏰ NEXT UP: {upcomingSchedule.CourseOffering.Subject.Code} - {upcomingSchedule.CourseOffering.Section.Name} starts at {upcomingSchedule.StartTime:hh\\:mm}.",
                                    CssClass = "alert-warning",
                                    IconClass = "bi-clock-history",
                                    SubjectCode = upcomingSchedule.CourseOffering.Subject.Code,
                                    SubjectName = upcomingSchedule.CourseOffering.Subject.Name,
                                    BatchName = upcomingSchedule.CourseOffering.Batch.Name,
                                    SectionName = upcomingSchedule.CourseOffering.Section.Name,
                                    UpcomingStartTime = upcomingSchedule.StartTime,
                                    SecondsUntilStart = secondsUntilStart
                                };
                            }
                            else
                            {
                                // Class is active but attendance already marked
                                classStatus = new CurrentClassStatus
                                {
                                    Status = ClassStatusType.None,
                                    Message = $"✅ Attendance already marked for {upcomingSchedule.CourseOffering.Subject.Code} - {upcomingSchedule.CourseOffering.Section.Name}.",
                                    CssClass = "alert-success",
                                    IconClass = "bi-check-circle-fill"
                                };
                            }
                        }
                        else
                        {
                            // All classes finished for today
                            classStatus = new CurrentClassStatus
                            {
                                Status = ClassStatusType.None,
                                Message = "All classes for today have finished.",
                                CssClass = "alert-info",
                                IconClass = "bi-check-circle"
                            };
                        }
                    }
                }
            }

            var viewModel = new TeacherDashboardViewModel
            {
                CourseOfferings = courseOfferings,
                ClassStatus = classStatus
            };

            return View(viewModel);
        }

        // GET: Teacher/QuickMarkAttendance (Smart Quick Access)
        public async Task<IActionResult> QuickMarkAttendance()
        {
            // Find the currently active class (same logic as dashboard)
            var now = GetInstituteTime();
            var today = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            // Get all course offerings assigned to this teacher
            var courseOfferingIds = await _context.CourseOfferings
                .Where(co => co.TeacherId == CurrentUserId)
                .Select(co => co.Id)
                .ToListAsync();

            if (!courseOfferingIds.Any())
            {
                SetErrorMessage("You don't have any course offerings assigned.");
                return RedirectToAction(nameof(Index));
            }

            // Get all class schedules for today
            var todaySchedules = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Where(cs => courseOfferingIds.Contains(cs.CourseOfferingId) && cs.DayOfWeek == today)
                .OrderBy(cs => cs.StartTime)
                .ToListAsync();

            // Find active schedule
            foreach (var schedule in todaySchedules)
            {
                var graceEndTime = schedule.StartTime.Add(TimeSpan.FromMinutes(schedule.GracePeriodMinutes));
                
                if (currentTime >= schedule.StartTime && currentTime <= graceEndTime)
                {
                    // Check if attendance already marked for THIS SPECIFIC SESSION
                    var alreadyMarked = await _context.AttendanceRecords
                        .AnyAsync(ar => ar.ClassScheduleId == schedule.Id);
                    
                    if (!alreadyMarked)
                    {
                        // Found valid active class - redirect to mark attendance with ClassScheduleId
                        return RedirectToAction(nameof(MarkAttendance), new { id = schedule.Id });
                    }
                }
            }

            // No valid class found - show portal closed
            var closedViewModel = new AttendancePortalClosedViewModel
            {
                SubjectCode = "",
                SubjectName = "",
                BatchName = "",
                SectionName = "",
                ScheduledDay = today,
                CurrentTime = now,
                Reason = "No active class with open attendance portal at this time. Please check the dashboard for your schedule."
            };
            
            return View("AttendancePortalClosed", closedViewModel);
        }

        // GET: Teacher/MarkAttendance/5 (Time-Lock Engine - Per Session)
        // Parameter 'id' is now ClassScheduleId (not CourseOfferingId)
        public async Task<IActionResult> MarkAttendance(int id)
        {
            // Get the class schedule with all related data
            var classSchedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            // Security: Verify schedule exists and belongs to current teacher
            if (classSchedule == null || 
                classSchedule.CourseOffering.TeacherId != CurrentUserId || 
                !BelongsToCurrentInstitute(classSchedule.InstituteId))
            {
                SetErrorMessage("Unauthorized access or schedule not found.");
                return RedirectToAction(nameof(Index));
            }

            var courseOffering = classSchedule.CourseOffering;

            // ===== TIME-LOCK LOGIC =====
            var now = GetInstituteTime();
            var today = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            // Verify this schedule is for today
            if (classSchedule.DayOfWeek != today)
            {
                var closedViewModel = new AttendancePortalClosedViewModel
                {
                    SubjectCode = courseOffering.Subject.Code,
                    SubjectName = courseOffering.Subject.Name,
                    BatchName = courseOffering.Batch.Name,
                    SectionName = courseOffering.Section.Name,
                    ScheduledDay = classSchedule.DayOfWeek,
                    CurrentTime = now,
                    Reason = $"This class is scheduled for {classSchedule.DayOfWeek}, not today ({today})."
                };
                return View("AttendancePortalClosed", closedViewModel);
            }

            // Calculate grace period window
            var graceEndTime = classSchedule.StartTime.Add(TimeSpan.FromMinutes(classSchedule.GracePeriodMinutes));

            // Check if current time is within the allowed window
            // Rule: currentTime >= StartTime AND currentTime <= (StartTime + GracePeriodMinutes)
            if (currentTime < classSchedule.StartTime || currentTime > graceEndTime)
            {
                var closedViewModel = new AttendancePortalClosedViewModel
                {
                    SubjectCode = courseOffering.Subject.Code,
                    SubjectName = courseOffering.Subject.Name,
                    BatchName = courseOffering.Batch.Name,
                    SectionName = courseOffering.Section.Name,
                    ScheduledDay = today,
                    StartTime = classSchedule.StartTime,
                    EndTime = classSchedule.EndTime,
                    GracePeriodMinutes = classSchedule.GracePeriodMinutes,
                    CurrentTime = now,
                    Reason = $"Attendance can only be marked between {classSchedule.StartTime:hh\\:mm} and {graceEndTime:hh\\:mm}. Current time: {currentTime:hh\\:mm}."
                };
                return View("AttendancePortalClosed", closedViewModel);
            }

            // ===== TIME WINDOW VALID - LOAD STUDENTS =====
            
            // Check if attendance already marked for THIS SPECIFIC SESSION
            var alreadyMarked = await _context.AttendanceRecords
                .AnyAsync(ar => ar.ClassScheduleId == id);

            if (alreadyMarked)
            {
                SetErrorMessage($"Attendance for this session has already been marked.");
                return RedirectToAction(nameof(Index));
            }

            // Load enrolled students - CRUCIAL: Order by RollNumber
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Where(se => se.CourseOfferingId == courseOffering.Id)
                .OrderBy(se => se.Student.RollNumber)
                .ToListAsync();

            // Build ViewModel
            var viewModel = new MarkAttendanceViewModel
            {
                CourseOfferingId = courseOffering.Id,
                ClassScheduleId = classSchedule.Id,
                SubjectCode = courseOffering.Subject.Code,
                SubjectName = courseOffering.Subject.Name,
                BatchName = courseOffering.Batch.Name,
                SectionName = courseOffering.Section.Name,
                Date = GetInstituteTime().Date,
                DayOfWeek = today,
                StartTime = classSchedule.StartTime,
                EndTime = classSchedule.EndTime,
                Students = enrollments.Select(e => new StudentAttendanceInputModel
                {
                    StudentEnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    RollNumber = e.Student.RollNumber ?? "N/A",
                    StudentName = e.Student.FullName,
                    Status = AttendanceStatus.Present // Default to Present
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Teacher/MarkAttendance (SECURE: Server-Side Time Validation)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(MarkAttendanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Invalid form data.");
                return RedirectToAction(nameof(Index));
            }

            // ===== VALIDATION 1: FETCH SCHEDULE =====
            var classSchedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .FirstOrDefaultAsync(cs => cs.Id == model.ClassScheduleId);

            if (classSchedule == null)
            {
                SetErrorMessage("Class schedule not found.");
                return RedirectToAction(nameof(Index));
            }

            // ===== VALIDATION 2: TEACHER OWNERSHIP (SECURITY) =====
            if (classSchedule.CourseOffering.TeacherId != CurrentUserId || 
                !BelongsToCurrentInstitute(classSchedule.InstituteId))
            {
                SetErrorMessage("Unauthorized access. This schedule does not belong to you.");
                return RedirectToAction(nameof(Index));
            }

            // ===== VALIDATION 3: DUPLICATE CHECK =====
            var alreadyMarked = await _context.AttendanceRecords
                .AnyAsync(ar => ar.ClassScheduleId == model.ClassScheduleId);

            if (alreadyMarked)
            {
                SetErrorMessage($"Attendance for this session has already been marked.");
                return RedirectToAction(nameof(Index));
            }

            // ===== VALIDATION 4: DAY OF WEEK CHECK =====
            var now = GetInstituteTime();
            var today = now.DayOfWeek;
            
            if (classSchedule.DayOfWeek != today)
            {
                var closedViewModel = new AttendancePortalClosedViewModel
                {
                    SubjectCode = classSchedule.CourseOffering.Subject.Code,
                    SubjectName = classSchedule.CourseOffering.Subject.Name,
                    BatchName = classSchedule.CourseOffering.Batch.Name,
                    SectionName = classSchedule.CourseOffering.Section.Name,
                    ScheduledDay = classSchedule.DayOfWeek,
                    StartTime = classSchedule.StartTime,
                    EndTime = classSchedule.EndTime,
                    GracePeriodMinutes = classSchedule.GracePeriodMinutes,
                    CurrentTime = now,
                    Reason = $"This class is scheduled for {classSchedule.DayOfWeek}, but today is {today}. You cannot submit attendance for a different day."
                };
                return View("AttendancePortalClosed", closedViewModel);
            }

            // ===== VALIDATION 5: TIME WINDOW CHECK (CRITICAL SECURITY) =====
            var currentTime = now.TimeOfDay;
            var graceEndTime = classSchedule.StartTime.Add(TimeSpan.FromMinutes(classSchedule.GracePeriodMinutes));

            // Rule: currentTime >= StartTime AND currentTime <= (StartTime + GracePeriodMinutes)
            if (currentTime < classSchedule.StartTime || currentTime > graceEndTime)
            {
                var closedViewModel = new AttendancePortalClosedViewModel
                {
                    SubjectCode = classSchedule.CourseOffering.Subject.Code,
                    SubjectName = classSchedule.CourseOffering.Subject.Name,
                    BatchName = classSchedule.CourseOffering.Batch.Name,
                    SectionName = classSchedule.CourseOffering.Section.Name,
                    ScheduledDay = today,
                    StartTime = classSchedule.StartTime,
                    EndTime = classSchedule.EndTime,
                    GracePeriodMinutes = classSchedule.GracePeriodMinutes,
                    CurrentTime = now,
                    Reason = $"SUBMISSION REJECTED: The time window for marking attendance has expired. " +
                             $"Attendance can only be submitted between {classSchedule.StartTime:hh\\:mm} and {graceEndTime:hh\\:mm}. " +
                             $"Current time: {currentTime:hh\\:mm}. Your form data has NOT been saved."
                };
                return View("AttendancePortalClosed", closedViewModel);
            }

            // ===== ALL VALIDATIONS PASSED - PROCEED WITH SAVE =====
            try
            {
                var utcNow = DateTime.UtcNow;
                var attendanceRecords = new List<AttendanceRecord>();

                // Create attendance records for all students
                foreach (var student in model.Students)
                {
                    var record = new AttendanceRecord
                    {
                        InstituteId = CurrentInstituteId,
                        StudentEnrollmentId = student.StudentEnrollmentId,
                        CourseOfferingId = model.CourseOfferingId,
                        ClassScheduleId = model.ClassScheduleId,
                        Date = model.Date,
                        Status = student.Status,
                        MarkedByTeacherId = CurrentUserId,
                        MarkedAt = utcNow,
                        CreatedAt = utcNow
                    };

                    attendanceRecords.Add(record);
                }

                _context.AttendanceRecords.AddRange(attendanceRecords);
                await _context.SaveChangesAsync();

                // Count statistics
                var presentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Present);
                var absentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Absent);
                var leaveCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Leave);

                SetSuccessMessage(
                    $"✅ Attendance marked successfully for {attendanceRecords.Count} student(s). " +
                    $"Present: {presentCount}, Absent: {absentCount}, Leave: {leaveCount}.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error marking attendance: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
