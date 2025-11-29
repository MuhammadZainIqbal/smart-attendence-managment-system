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
    /// Manages Course Offerings (Teacher-Subject assignments for specific Batch+Section).
    /// Implements bi-directional auto-enrollment:
    /// - When a CourseOffering is created, all existing students in that Batch+Section are auto-enrolled.
    /// </summary>
    public class CourseOfferingsController : BaseAdminController
    {
        public CourseOfferingsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: CourseOfferings
        public async Task<IActionResult> Index()
        {
            var courseOfferings = await _context.CourseOfferings
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Include(co => co.Subject)
                .Include(co => co.Teacher)
                .OrderBy(co => co.Batch.Name)
                .ThenBy(co => co.Section.Name)
                .ThenBy(co => co.Subject.Code)
                .ToListAsync();

            return View(courseOfferings);
        }

        // GET: CourseOfferings/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: CourseOfferings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int batchId,
            int sectionId,
            int subjectId,
            string teacherId)
        {
            if (batchId == 0 || sectionId == 0 || subjectId == 0 || string.IsNullOrWhiteSpace(teacherId))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                await PopulateDropdowns();
                return View();
            }

            // STRICT VALIDATION: Check if ANY teacher is already assigned to this Subject + Batch + Section
            var existingOffering = await _context.CourseOfferings
                .Include(co => co.Subject)
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Include(co => co.Teacher)
                .FirstOrDefaultAsync(co =>
                    co.InstituteId == CurrentInstituteId &&
                    co.BatchId == batchId &&
                    co.SectionId == sectionId &&
                    co.SubjectId == subjectId);

            if (existingOffering != null)
            {
                // Build specific error message showing existing assignment
                var errorMessage = $"{existingOffering.Subject.Name} is already assigned to " +
                                   $"{existingOffering.Teacher.FullName} for " +
                                   $"Batch {existingOffering.Batch.Name} - Section {existingOffering.Section.Name}.";
                
                ModelState.AddModelError(string.Empty, errorMessage);
                await PopulateDropdowns();
                return View();
            }

            // Check if teacher belongs to current institute
            var teacher = await _userManager.FindByIdAsync(teacherId);
            if (teacher == null || teacher.InstituteId != CurrentInstituteId)
            {
                ModelState.AddModelError(string.Empty, "Invalid teacher selected.");
                await PopulateDropdowns();
                return View();
            }

            try
            {
                // Create the CourseOffering
                var courseOffering = new CourseOffering
                {
                    BatchId = batchId,
                    SectionId = sectionId,
                    SubjectId = subjectId,
                    TeacherId = teacherId,
                    InstituteId = CurrentInstituteId
                };

                _context.CourseOfferings.Add(courseOffering);
                await _context.SaveChangesAsync();

                // ===== BI-DIRECTIONAL AUTO-ENROLLMENT =====
                // Find all students in this Batch + Section by their BatchId and SectionId properties
                var allUsers = await _context.Users
                    .Where(u => u.InstituteId == CurrentInstituteId && 
                                u.BatchId == batchId && 
                                u.SectionId == sectionId)
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

                // Auto-enroll these students in the new CourseOffering
                int enrollmentCount = 0;
                foreach (var student in students)
                {
                    // Check if enrollment already exists (safety check)
                    var existingEnrollment = await _context.StudentEnrollments
                        .FirstOrDefaultAsync(se =>
                            se.StudentId == student.Id &&
                            se.CourseOfferingId == courseOffering.Id);

                    if (existingEnrollment == null)
                    {
                        var enrollment = new StudentEnrollment
                        {
                            StudentId = student.Id,
                            CourseOfferingId = courseOffering.Id,
                            InstituteId = CurrentInstituteId,
                            EnrolledAt = DateTime.UtcNow
                        };
                        _context.StudentEnrollments.Add(enrollment);
                        enrollmentCount++;
                    }
                }

                await _context.SaveChangesAsync();

                // Get course details for success message
                var batch = await _context.Batches.FindAsync(batchId);
                var section = await _context.Sections.FindAsync(sectionId);
                var subject = await _context.Subjects.FindAsync(subjectId);

                SetSuccessMessage(
                    $"Course Offering created successfully! " +
                    $"{subject?.Code} - {batch?.Name} - {section?.Name}. " +
                    $"Auto-enrolled {enrollmentCount} existing student(s).");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error creating course offering: {ex.Message}");
                await PopulateDropdowns();
                return View();
            }
        }

        // GET: CourseOfferings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseOffering = await _context.CourseOfferings
                .Include(co => co.Batch)
                .Include(co => co.Section)
                .Include(co => co.Subject)
                .Include(co => co.Teacher)
                .FirstOrDefaultAsync(co => co.Id == id);

            if (courseOffering == null || !BelongsToCurrentInstitute(courseOffering.InstituteId))
            {
                return NotFound();
            }

            return View(courseOffering);
        }

        // POST: CourseOfferings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseOffering = await _context.CourseOfferings.FindAsync(id);

            if (courseOffering == null || !BelongsToCurrentInstitute(courseOffering.InstituteId))
            {
                return NotFound();
            }

            try
            {
                // Delete associated enrollments first
                var enrollments = await _context.StudentEnrollments
                    .Where(se => se.CourseOfferingId == id)
                    .ToListAsync();

                _context.StudentEnrollments.RemoveRange(enrollments);

                // Delete the course offering
                _context.CourseOfferings.Remove(courseOffering);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Course Offering deleted successfully. Removed {enrollments.Count} student enrollment(s).");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("Cannot delete this course offering. It may have associated class schedules or attendance records.");
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates dropdowns for Create form.
        /// All dropdowns are filtered by CurrentInstituteId.
        /// </summary>
        private async Task PopulateDropdowns()
        {
            // Teachers dropdown - show name and email
            var allUsers = await _userManager.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var teachers = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Teacher"))
                {
                    teachers.Add(user);
                }
            }

            ViewBag.Teachers = new SelectList(
                teachers.Select(t => new
                {
                    Id = t.Id,
                    DisplayName = $"{t.FullName} ({t.Email})"
                }),
                "Id",
                "DisplayName"
            );

            // Batches dropdown
            var batches = await _context.Batches
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.Batches = new SelectList(batches, "Id", "Name");

            // Sections dropdown
            var sections = await _context.Sections
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Sections = new SelectList(sections, "Id", "Name");

            // Subjects dropdown - show code and name
            var subjects = await _context.Subjects
                .OrderBy(s => s.Code)
                .ToListAsync();

            ViewBag.Subjects = new SelectList(
                subjects.Select(s => new
                {
                    Id = s.Id,
                    DisplayName = $"{s.Code} - {s.Name}"
                }),
                "Id",
                "DisplayName"
            );
        }
    }
}
