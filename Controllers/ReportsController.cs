using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Teacher Reporting & Analytics Module.
    /// Provides data visualization and detailed attendance reports.
    /// Security: ONLY Teachers can access.
    /// </summary>
    [Authorize(Roles = "Teacher")]
    public class ReportsController : BaseTeacherController
    {
        public ReportsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Reports (Report Hub - Landing Page)
        public async Task<IActionResult> Index()
        {
            // Fetch all CourseOfferings assigned to this teacher
            var courses = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Where(co => co.TeacherId == CurrentUserId)
                .OrderBy(co => co.Subject.Code)
                .ToListAsync();

            var viewModel = new ReportsIndexViewModel();

            foreach (var course in courses)
            {
                var studentCount = await _context.StudentEnrollments
                    .CountAsync(se => se.CourseOfferingId == course.Id);

                viewModel.Courses.Add(new TeacherCourseViewModel
                {
                    CourseOfferingId = course.Id,
                    SubjectCode = course.Subject.Code,
                    SubjectName = course.Subject.Name,
                    BatchName = course.Batch.Name,
                    SectionName = course.Section.Name,
                    TotalStudents = studentCount
                });
            }

            return View(viewModel);
        }

        // GET: Reports/CourseReport/5 (Class Analytics)
        public async Task<IActionResult> CourseReport(int id)
        {
            // Fetch CourseOffering with navigation properties
            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (courseOffering == null || courseOffering.TeacherId != CurrentUserId)
            {
                SetErrorMessage("Unauthorized access or course not found.");
                return RedirectToAction(nameof(Index));
            }

            // Calculate Total Classes Held (distinct ClassScheduleId + Date combinations)
            var totalClassesHeld = await _context.AttendanceRecords
                .Where(ar => ar.CourseOfferingId == id)
                .Select(ar => new { ar.ClassScheduleId, ar.Date })
                .Distinct()
                .CountAsync();

            if (totalClassesHeld == 0)
            {
                SetErrorMessage("No attendance records found for this course yet.");
                return RedirectToAction(nameof(Index));
            }

            // Fetch all StudentEnrollments for this course
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Where(se => se.CourseOfferingId == id)
                .OrderBy(se => se.Student.RollNumber)
                .ToListAsync();

            var studentStats = new List<StudentStatsViewModel>();
            int greenCount = 0, yellowCount = 0, redCount = 0;
            double totalPercentage = 0;

            foreach (var enrollment in enrollments)
            {
                // Fetch attendance records for this student in this course
                var records = await _context.AttendanceRecords
                    .Where(ar => ar.StudentEnrollmentId == enrollment.Id)
                    .ToListAsync();

                int presentCount = records.Count(r => r.Status == AttendanceStatus.Present);
                int absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
                int leaveCount = records.Count(r => r.Status == AttendanceStatus.Leave);

                // Attendance Percentage: (Present / Total Classes) * 100
                double attendancePercentage = totalClassesHeld > 0
                    ? (double)presentCount / totalClassesHeld * 100
                    : 0;

                totalPercentage += attendancePercentage;

                // Categorize into Green/Yellow/Red
                if (attendancePercentage >= 85)
                    greenCount++;
                else if (attendancePercentage >= 70)
                    yellowCount++;
                else
                    redCount++;

                studentStats.Add(new StudentStatsViewModel
                {
                    EnrollmentId = enrollment.Id,
                    RollNumber = enrollment.Student?.RollNumber ?? string.Empty,
                    StudentName = enrollment.Student?.FullName ?? "Unknown",
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LeaveCount = leaveCount,
                    TotalClasses = totalClassesHeld,
                    AttendancePercentage = Math.Round(attendancePercentage, 2)
                });
            }

            // Calculate Class Average Percentage
            double classAverage = enrollments.Count > 0
                ? totalPercentage / enrollments.Count
                : 0;

            // Prepare Chart Data for Distribution (Pie Chart)
            var chartData = new ChartDataViewModel
            {
                Labels = new List<string> { "Excellent (≥85%)", "Good (70-84%)", "Poor (<70%)" },
                Data = new List<int> { greenCount, yellowCount, redCount },
                BackgroundColors = new List<string> { "#28a745", "#ffc107", "#dc3545" }
            };

            var viewModel = new CourseReportViewModel
            {
                CourseOfferingId = courseOffering.Id,
                SubjectCode = courseOffering.Subject.Code,
                SubjectName = courseOffering.Subject.Name,
                BatchName = courseOffering.Batch.Name,
                SectionName = courseOffering.Section.Name,
                TotalClassesHeld = totalClassesHeld,
                ClassAveragePercentage = Math.Round(classAverage, 2),
                GreenCount = greenCount,
                YellowCount = yellowCount,
                RedCount = redCount,
                StudentStats = studentStats,
                ChartData = chartData
            };

            return View(viewModel);
        }

        // GET: Reports/StudentReport/5 (Individual Deep Dive)
        public async Task<IActionResult> StudentReport(int id)
        {
            // Fetch StudentEnrollment with navigation properties
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
                SetErrorMessage("Unauthorized access or enrollment not found.");
                return RedirectToAction(nameof(Index));
            }

            // Fetch all AttendanceRecords for this enrollment
            var attendanceRecords = await _context.AttendanceRecords
                .Include(ar => ar.ClassSchedule)
                .Where(ar => ar.StudentEnrollmentId == id)
                .OrderByDescending(ar => ar.Date)
                .ToListAsync();

            int presentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Present);
            int absentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Absent);
            int leaveCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Leave);
            int totalClasses = attendanceRecords.Count;

            // Attendance Percentage: (Present / Total) * 100
            double attendancePercentage = totalClasses > 0
                ? (double)presentCount / totalClasses * 100
                : 0;

            // Prepare Chart Data (Pie Chart: Present vs Absent vs Leave)
            var chartData = new ChartDataViewModel
            {
                Labels = new List<string> { "Present", "Absent", "Leave" },
                Data = new List<int> { presentCount, absentCount, leaveCount },
                BackgroundColors = new List<string> { "#28a745", "#dc3545", "#ffc107" }
            };

            // Prepare detailed history
            var attendanceHistory = new List<AttendanceRecordViewModel>();
            foreach (var record in attendanceRecords)
            {
                attendanceHistory.Add(new AttendanceRecordViewModel
                {
                    Date = record.Date,
                    DayOfWeek = record.Date.DayOfWeek.ToString(),
                    Status = record.Status,
                    TimeSlot = $"{record.ClassSchedule.StartTime:hh\\:mm} - {record.ClassSchedule.EndTime:hh\\:mm}",
                    MarkedAt = record.MarkedAt
                });
            }

            var viewModel = new StudentDetailReportViewModel
            {
                EnrollmentId = enrollment.Id,
                CourseOfferingId = enrollment.CourseOfferingId, // For "Back to Course" navigation
                StudentName = enrollment.Student?.FullName ?? "Unknown",
                RollNumber = enrollment.Student?.RollNumber ?? string.Empty,
                SubjectCode = enrollment.CourseOffering.Subject.Code,
                SubjectName = enrollment.CourseOffering.Subject.Name,
                BatchName = enrollment.CourseOffering.Batch.Name,
                SectionName = enrollment.CourseOffering.Section.Name,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                LeaveCount = leaveCount,
                TotalClasses = totalClasses,
                AttendancePercentage = Math.Round(attendancePercentage, 2),
                AttendanceHistory = attendanceHistory,
                ChartData = chartData
            };

            return View(viewModel);
        }

        // GET: Reports/ExportCourseReport/5 (Export Course-Wise Report to Excel)
        public async Task<IActionResult> ExportCourseReport(int id)
        {
            // Fetch CourseOffering with navigation properties
            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (courseOffering == null || courseOffering.TeacherId != CurrentUserId)
            {
                SetErrorMessage("Unauthorized access or course not found.");
                return RedirectToAction(nameof(Index));
            }

            // Calculate Total Classes Held
            var totalClassesHeld = await _context.AttendanceRecords
                .Where(ar => ar.CourseOfferingId == id)
                .Select(ar => new { ar.ClassScheduleId, ar.Date })
                .Distinct()
                .CountAsync();

            if (totalClassesHeld == 0)
            {
                SetErrorMessage("No attendance records found for this course yet.");
                return RedirectToAction(nameof(Index));
            }

            // Fetch all StudentEnrollments for this course
            var enrollments = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Where(se => se.CourseOfferingId == id)
                .OrderBy(se => se.Student.RollNumber)
                .ToListAsync();

            // Create Excel Workbook
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Summary");

                // ===== HEADER SECTION =====
                worksheet.Cell("A1").Value = $"Attendance Report - {courseOffering.Subject.Name}";
                worksheet.Range("A1:G1").Merge();
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;
                worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4788"); // Dark Blue
                worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;

                // Course Details (Row 2)
                worksheet.Cell("A2").Value = $"Batch: {courseOffering.Batch.Name} | Section: {courseOffering.Section.Name} | Subject: {courseOffering.Subject.Code}";
                worksheet.Range("A2:G2").Merge();
                worksheet.Cell("A2").Style.Font.Bold = true;
                worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.LightGray;

                // Date (Row 3)
                worksheet.Cell("A3").Value = $"Generated On: {DateTime.Now:dd-MMM-yyyy HH:mm}";
                worksheet.Range("A3:G3").Merge();
                worksheet.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A3").Style.Font.Italic = true;

                // ===== TABLE HEADERS (Row 5) =====
                int currentRow = 5;
                worksheet.Cell(currentRow, 1).Value = "Roll No";
                worksheet.Cell(currentRow, 2).Value = "Student Name";
                worksheet.Cell(currentRow, 3).Value = "Total Classes";
                worksheet.Cell(currentRow, 4).Value = "Present";
                worksheet.Cell(currentRow, 5).Value = "Absent";
                worksheet.Cell(currentRow, 6).Value = "Leave";
                worksheet.Cell(currentRow, 7).Value = "Percentage";
                worksheet.Cell(currentRow, 8).Value = "Status";

                // Style Table Headers
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4788"); // Dark Blue
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // ===== DATA ROWS =====
                currentRow++;
                foreach (var enrollment in enrollments)
                {
                    // Fetch attendance records for this student
                    var records = await _context.AttendanceRecords
                        .Where(ar => ar.StudentEnrollmentId == enrollment.Id)
                        .ToListAsync();

                    int presentCount = records.Count(r => r.Status == AttendanceStatus.Present);
                    int absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
                    int leaveCount = records.Count(r => r.Status == AttendanceStatus.Leave);

                    double attendancePercentage = totalClassesHeld > 0
                        ? (double)presentCount / totalClassesHeld * 100
                        : 0;

                    string status;
                    XLColor statusColor;

                    if (attendancePercentage >= 85)
                    {
                        status = "Excellent";
                        statusColor = XLColor.LightGreen;
                    }
                    else if (attendancePercentage >= 70)
                    {
                        status = "Warning";
                        statusColor = XLColor.FromHtml("#FFEB9C"); // Light Yellow
                    }
                    else
                    {
                        status = "Critical";
                        statusColor = XLColor.FromHtml("#FFC7CE"); // Light Red
                    }

                    // Populate row
                    worksheet.Cell(currentRow, 1).Value = enrollment.Student.RollNumber;
                    worksheet.Cell(currentRow, 2).Value = enrollment.Student.FullName;
                    worksheet.Cell(currentRow, 3).Value = totalClassesHeld;
                    worksheet.Cell(currentRow, 4).Value = presentCount;
                    worksheet.Cell(currentRow, 5).Value = absentCount;
                    worksheet.Cell(currentRow, 6).Value = leaveCount;
                    worksheet.Cell(currentRow, 7).Value = $"{Math.Round(attendancePercentage, 2)}%";
                    worksheet.Cell(currentRow, 8).Value = status;

                    // Color-code Status cell
                    worksheet.Cell(currentRow, 8).Style.Fill.BackgroundColor = statusColor;
                    worksheet.Cell(currentRow, 8).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Center align numeric columns
                    worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add borders
                    var rowRange = worksheet.Range(currentRow, 1, currentRow, 8);
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    currentRow++;
                }

                // ===== AUTO-FIT COLUMNS =====
                worksheet.Columns().AdjustToContents();

                // ===== GENERATE FILE =====
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    // File Name: Attendance_BatchName_SectionName_SubjectName_YYYY-MM-DD.xlsx
                    string fileName = $"Attendance_{courseOffering.Batch.Name}_{courseOffering.Section.Name}_{courseOffering.Subject.Code}_{DateTime.Now:yyyy-MM-dd}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }

        // GET: Reports/ExportStudentReport/5 (Export Student-Wise Report to Excel)
        public async Task<IActionResult> ExportStudentReport(int id)
        {
            // Fetch StudentEnrollment with navigation properties
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
                SetErrorMessage("Unauthorized access or enrollment not found.");
                return RedirectToAction(nameof(Index));
            }

            // Fetch all AttendanceRecords for this enrollment
            var attendanceRecords = await _context.AttendanceRecords
                .Include(ar => ar.ClassSchedule)
                .Include(ar => ar.MarkedByTeacher)
                .Where(ar => ar.StudentEnrollmentId == id)
                .OrderByDescending(ar => ar.Date)
                .ToListAsync();

            if (!attendanceRecords.Any())
            {
                SetErrorMessage("No attendance records found for this student yet.");
                return RedirectToAction(nameof(Index));
            }

            // Create Excel Workbook
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Detailed History");

                // ===== HEADER SECTION =====
                worksheet.Cell("A1").Value = $"Student Attendance Report: {enrollment.Student.FullName}";
                worksheet.Range("A1:F1").Merge();
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;
                worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4788"); // Dark Blue
                worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;

                // Student Details (Row 2)
                worksheet.Cell("A2").Value = $"Roll No: {enrollment.Student.RollNumber} | Subject: {enrollment.CourseOffering.Subject.Code} - {enrollment.CourseOffering.Subject.Name}";
                worksheet.Range("A2:F2").Merge();
                worksheet.Cell("A2").Style.Font.Bold = true;
                worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.LightGray;

                // Summary Stats (Row 3)
                int presentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Present);
                int absentCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Absent);
                int leaveCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Leave);
                int totalClasses = attendanceRecords.Count;
                double attendancePercentage = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 0;

                worksheet.Cell("A3").Value = $"Present: {presentCount} | Absent: {absentCount} | Leave: {leaveCount} | Total: {totalClasses} | Percentage: {Math.Round(attendancePercentage, 2)}%";
                worksheet.Range("A3:F3").Merge();
                worksheet.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A3").Style.Font.Bold = true;

                // Date (Row 4)
                worksheet.Cell("A4").Value = $"Generated On: {DateTime.Now:dd-MMM-yyyy HH:mm}";
                worksheet.Range("A4:F4").Merge();
                worksheet.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell("A4").Style.Font.Italic = true;

                // ===== TABLE HEADERS (Row 6) =====
                int currentRow = 6;
                worksheet.Cell(currentRow, 1).Value = "Date";
                worksheet.Cell(currentRow, 2).Value = "Day";
                worksheet.Cell(currentRow, 3).Value = "Time Slot";
                worksheet.Cell(currentRow, 4).Value = "Status";
                worksheet.Cell(currentRow, 5).Value = "Marked By";
                worksheet.Cell(currentRow, 6).Value = "Marked At";

                // Style Table Headers
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4788"); // Dark Blue
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // ===== DATA ROWS =====
                currentRow++;
                foreach (var record in attendanceRecords)
                {
                    XLColor statusColor;
                    if (record.Status == AttendanceStatus.Present)
                        statusColor = XLColor.LightGreen;
                    else if (record.Status == AttendanceStatus.Leave)
                        statusColor = XLColor.FromHtml("#FFEB9C"); // Light Yellow
                    else
                        statusColor = XLColor.FromHtml("#FFC7CE"); // Light Red

                    // Populate row
                    worksheet.Cell(currentRow, 1).Value = record.Date.ToString("dd-MMM-yyyy");
                    worksheet.Cell(currentRow, 2).Value = record.Date.DayOfWeek.ToString();
                    worksheet.Cell(currentRow, 3).Value = $"{record.ClassSchedule.StartTime:hh\\:mm} - {record.ClassSchedule.EndTime:hh\\:mm}";
                    worksheet.Cell(currentRow, 4).Value = record.Status.ToString();
                    worksheet.Cell(currentRow, 5).Value = record.MarkedByTeacher?.FullName ?? "System";
                    worksheet.Cell(currentRow, 6).Value = record.MarkedAt.ToString("dd-MMM-yyyy HH:mm");

                    // Color-code Status cell
                    worksheet.Cell(currentRow, 4).Style.Fill.BackgroundColor = statusColor;
                    worksheet.Cell(currentRow, 4).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Center align columns
                    worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add borders
                    var rowRange = worksheet.Range(currentRow, 1, currentRow, 6);
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                    currentRow++;
                }

                // ===== AUTO-FIT COLUMNS =====
                worksheet.Columns().AdjustToContents();

                // ===== GENERATE FILE =====
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    // File Name: StudentHistory_RollNo_Name_YYYY-MM-DD.xlsx
                    string fileName = $"StudentHistory_{enrollment.Student.RollNumber}_{enrollment.Student.FullName.Replace(" ", "")}_{DateTime.Now:yyyy-MM-dd}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }
    }
}
