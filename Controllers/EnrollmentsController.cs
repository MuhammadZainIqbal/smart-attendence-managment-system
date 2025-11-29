using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Manual/Repeater Student Enrollments.
    /// Allows Admin to enroll students in courses outside their main Batch+Section.
    /// Use case: Repeater students taking courses from different sections.
    /// </summary>
    public class EnrollmentsController : BaseAdminController
    {
        public EnrollmentsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Enrollments
        public async Task<IActionResult> Index(int? batchId, int? sectionId)
        {
            // Populate Batch and Section dropdowns for filtering
            ViewData["BatchId"] = new SelectList(
                await _context.Batches
                    .Where(b => b.InstituteId == CurrentInstituteId)
                    .OrderBy(b => b.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                batchId
            );

            ViewData["SectionId"] = new SelectList(
                await _context.Sections
                    .Where(s => s.InstituteId == CurrentInstituteId)
                    .OrderBy(s => s.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                sectionId
            );

            // Build query with filters
            var query = _context.StudentEnrollments
                .Include(se => se.Student)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .AsQueryable();

            // Apply filters (AND logic)
            if (batchId.HasValue)
            {
                query = query.Where(se => se.CourseOffering.BatchId == batchId.Value);
            }

            if (sectionId.HasValue)
            {
                query = query.Where(se => se.CourseOffering.SectionId == sectionId.Value);
            }

            var enrollments = await query
                .OrderBy(se => se.Student.FullName)
                .ThenBy(se => se.CourseOffering.Subject.Code)
                .ToListAsync();

            return View(enrollments);
        }

        // GET: Enrollments/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string studentId, int courseOfferingId)
        {
            if (string.IsNullOrWhiteSpace(studentId) || courseOfferingId == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select both a student and a course offering.");
                await PopulateDropdowns();
                return View();
            }

            // Verify student belongs to current institute
            var student = await _userManager.FindByIdAsync(studentId);
            if (student == null || student.InstituteId != CurrentInstituteId)
            {
                ModelState.AddModelError(string.Empty, "Invalid student selected.");
                await PopulateDropdowns();
                return View();
            }

            // Verify course offering belongs to current institute
            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .FirstOrDefaultAsync(co => co.Id == courseOfferingId);

            if (courseOffering == null || !BelongsToCurrentInstitute(courseOffering.InstituteId))
            {
                ModelState.AddModelError(string.Empty, "Invalid course offering selected.");
                await PopulateDropdowns();
                return View();
            }

            // Check if enrollment already exists
            var existingEnrollment = await _context.StudentEnrollments
                .FirstOrDefaultAsync(se =>
                    se.StudentId == studentId &&
                    se.CourseOfferingId == courseOfferingId);

            if (existingEnrollment != null)
            {
                ModelState.AddModelError(string.Empty,
                    "This student is already enrolled in this course offering.");
                await PopulateDropdowns();
                return View();
            }

            try
            {
                var enrollment = new StudentEnrollment
                {
                    StudentId = studentId,
                    CourseOfferingId = courseOfferingId,
                    InstituteId = CurrentInstituteId,
                    EnrolledAt = DateTime.UtcNow
                };

                _context.StudentEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                SetSuccessMessage(
                    $"Enrollment created successfully! {student.FullName} enrolled in " +
                    $"{courseOffering.Subject.Code} - {courseOffering.Batch.Name} - {courseOffering.Section.Name}.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error creating enrollment: {ex.Message}");
                await PopulateDropdowns();
                return View();
            }
        }

        // POST: Enrollments/Delete/5 (Un-enroll)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var enrollment = await _context.StudentEnrollments
                .Include(se => se.Student)
                .Include(se => se.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .FirstOrDefaultAsync(se => se.Id == id);

            if (enrollment == null || !BelongsToCurrentInstitute(enrollment.Student.InstituteId))
            {
                return NotFound();
            }

            try
            {
                _context.StudentEnrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                SetSuccessMessage(
                    $"Enrollment deleted successfully! {enrollment.Student.FullName} un-enrolled from " +
                    $"{enrollment.CourseOffering.Subject.Code}.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("Cannot delete this enrollment. It may have associated attendance records.");
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Populates dropdowns for Create form.
        /// All dropdowns are filtered by CurrentInstituteId.
        /// </summary>
        private async Task PopulateDropdowns()
        {
            // Students dropdown - show Roll Number, Name, and Email (sorted by Roll Number)
            var allUsers = await _userManager.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .OrderBy(u => u.RollNumber)
                .ToListAsync();

            var students = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Student"))
                {
                    students.Add(user);
                }
            }

            ViewBag.Students = new SelectList(
                students.Select(s => new
                {
                    Id = s.Id,
                    DisplayName = $"{s.RollNumber} - {s.FullName} ({s.Email})"
                }),
                "Id",
                "DisplayName"
            );

            // Course Offerings dropdown - show Subject + Batch + Section
            var courseOfferings = await _context.CourseOfferings
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Include(co => co.Subject)
                .OrderBy(co => co.Subject.Code)
                .ThenBy(co => co.Batch.Name)
                .ThenBy(co => co.Section.Name)
                .ToListAsync();

            ViewBag.CourseOfferings = new SelectList(
                courseOfferings.Select(co => new
                {
                    Id = co.Id,
                    DisplayName = $"{co.Subject.Code} - {co.Subject.Name} | {co.Batch.Name} - {co.Section.Name}"
                }),
                "Id",
                "DisplayName"
            );
        }
    }
}
