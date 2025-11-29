using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Helpers;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Students for the current Institute.
    /// Implements auto-enrollment: when a student is created and assigned to a Batch+Section,
    /// they are automatically enrolled in all existing CourseOfferings for that Batch+Section.
    /// </summary>
    public class StudentsController : BaseAdminController
    {
        private readonly IEmailSender _emailSender;

        public StudentsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IEmailSender emailSender)
            : base(userManager, context)
        {
            _emailSender = emailSender;
        }

        // GET: Students
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

            // Get all users in current institute with their Batch and Section
            var allUsers = await _context.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .Include(u => u.Batch)
                .Include(u => u.Section)
                .OrderBy(u => u.RollNumber)
                .ToListAsync();

            // Filter to only students
            var students = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Student"))
                {
                    students.Add(user);
                }
            }

            // Apply Batch/Section filters
            // Show students who have AT LEAST ONE enrollment in the selected Batch/Section
            if (batchId.HasValue || sectionId.HasValue)
            {
                var filteredStudents = new List<ApplicationUser>();

                foreach (var student in students)
                {
                    // Check if student has any enrollment matching the filter
                    var hasMatchingEnrollment = await _context.StudentEnrollments
                        .Include(se => se.CourseOffering)
                        .AnyAsync(se => se.StudentId == student.Id &&
                                       (!batchId.HasValue || se.CourseOffering.BatchId == batchId.Value) &&
                                       (!sectionId.HasValue || se.CourseOffering.SectionId == sectionId.Value));

                    if (hasMatchingEnrollment)
                    {
                        filteredStudents.Add(student);
                    }
                }

                students = filteredStudents;
            }

            return View(students);
        }

        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            // Populate Batch and Section dropdowns
            ViewBag.Batches = new SelectList(
                await _context.Batches.OrderBy(b => b.Name).ToListAsync(),
                "Id",
                "Name"
            );

            ViewBag.Sections = new SelectList(
                await _context.Sections.OrderBy(s => s.Name).ToListAsync(),
                "Id",
                "Name"
            );

            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email, string rollNumber, int batchId, int sectionId)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rollNumber))
            {
                ModelState.AddModelError(string.Empty, "Full Name, Email, and Roll Number are required.");
                await PopulateDropdowns();
                return View();
            }

            if (batchId <= 0 || sectionId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select both Batch and Section.");
                await PopulateDropdowns();
                return View();
            }

            // Validate Roll Number uniqueness (case-insensitive, per institute)
            var duplicateRollNumber = await _userManager.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .AnyAsync(u => u.RollNumber != null && u.RollNumber.ToLower() == rollNumber.ToLower());

            if (duplicateRollNumber)
            {
                ModelState.AddModelError("rollNumber", "This Roll Number already exists in your institute.");
                await PopulateDropdowns();
                return View();
            }

            // Verify Batch and Section belong to current Institute
            var batch = await _context.Batches.FindAsync(batchId);
            var section = await _context.Sections.FindAsync(sectionId);

            if (batch == null || section == null || 
                !BelongsToCurrentInstitute(batch.InstituteId) || 
                !BelongsToCurrentInstitute(section.InstituteId))
            {
                ModelState.AddModelError(string.Empty, "Invalid Batch or Section selection.");
                await PopulateDropdowns();
                return View();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("email", "This email is already registered.");
                await PopulateDropdowns();
                return View();
            }

            try
            {
                // Generate random password
                string randomPassword = PasswordGenerator.Generate(12);

                // Create new student user
                var student = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    RollNumber = rollNumber,
                    BatchId = batchId,
                    SectionId = sectionId,
                    InstituteId = CurrentInstituteId,
                    EmailConfirmed = true, // Auto-confirm for admin-created users
                    IsPasswordChanged = false // Force password change on first login
                };

                var result = await _userManager.CreateAsync(student, randomPassword);

                if (result.Succeeded)
                {
                    // Assign Student role
                    await _userManager.AddToRoleAsync(student, "Student");

                    // ===================================================================
                    // CRITICAL AUTO-ENROLLMENT LOGIC
                    // ===================================================================
                    // Query all CourseOfferings for this BatchId + SectionId
                    var courseOfferings = await _context.CourseOfferings
                        .Where(co => co.BatchId == batchId && co.SectionId == sectionId)
                        .ToListAsync();

                    // Create StudentEnrollment for each CourseOffering
                    foreach (var courseOffering in courseOfferings)
                    {
                        var enrollment = new StudentEnrollment
                        {
                            StudentId = student.Id,
                            CourseOfferingId = courseOffering.Id,
                            InstituteId = CurrentInstituteId,
                            EnrolledAt = DateTime.UtcNow
                        };

                        _context.StudentEnrollments.Add(enrollment);
                    }

                    await _context.SaveChangesAsync();

                    Console.WriteLine($"✅ Auto-enrolled student '{fullName}' in {courseOfferings.Count} courses.");

                    // Get Institute details for email
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.Id == CurrentInstituteId);

                    // Send welcome email with credentials
                    try
                    {
                        string emailSubject = $"Welcome to {institute?.Name} - Student Account Created";
                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #28a745;'>Welcome to Attendance Management System!</h2>
                                <p>Hello <strong>{student.FullName}</strong>,</p>
                                <p>Your Student account has been created for <strong>{institute?.Name}</strong>.</p>
                                
                                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                    <h3 style='margin-top: 0; color: #333;'>Your Login Credentials</h3>
                                    <p><strong>Institute Code:</strong> <span style='color: #ffc107; font-size: 1.2em;'>{institute?.Code}</span></p>
                                    <p><strong>Roll Number:</strong> <span style='color: #28a745; font-size: 1.2em; font-family: monospace;'>{rollNumber}</span></p>
                                    <p><strong>Email:</strong> {email}</p>
                                    <p><strong>Temporary Password:</strong> <span style='color: #007bff; font-size: 1.2em; font-family: monospace;'>{randomPassword}</span></p>
                                    <p><strong>Batch:</strong> {batch.Name}</p>
                                    <p><strong>Section:</strong> {section.Name}</p>
                                </div>

                                <div style='background-color: #d1ecf1; padding: 15px; border-left: 4px solid #0c5460; margin: 20px 0;'>
                                    <p style='margin: 0;'><strong>📚 Enrollment:</strong> You have been automatically enrolled in {courseOfferings.Count} course(s) for your section.</p>
                                </div>

                                <div style='background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                    <p style='margin: 0;'><strong>⚠️ Important:</strong> You will be required to change this password when you first log in.</p>
                                </div>

                                <p><strong>Login URL:</strong> <a href='https://localhost:44370/Account/Login'>Click here to login</a></p>

                                <hr>
                                <p>If you have any questions, please contact your administrator.</p>
                                <p>Best regards,<br>{institute?.Name} Administration</p>
                            </div>";

                        await _emailSender.SendEmailAsync(email, emailSubject, emailBody);
                        
                        SetSuccessMessage($"Student '{fullName}' created and enrolled in {courseOfferings.Count} course(s). Welcome email sent to {email}.");
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail the creation
                        Console.WriteLine($"Failed to send welcome email: {emailEx.Message}");
                        SetSuccessMessage($"Student '{fullName}' created and enrolled. WARNING: Email delivery failed. Password: {randomPassword}");
                    }

                    return RedirectToAction(nameof(Index));
                }

                // If creation failed
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }

            await PopulateDropdowns();
            return View();
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetErrorMessage("Invalid student ID.");
                return RedirectToAction(nameof(Index));
            }

            var student = await _userManager.FindByIdAsync(id);
            
            if (student == null)
            {
                SetErrorMessage("Student not found.");
                return RedirectToAction(nameof(Index));
            }

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(student.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Delete related enrollments first
                var enrollments = await _context.StudentEnrollments
                    .Where(e => e.StudentId == id)
                    .ToListAsync();

                _context.StudentEnrollments.RemoveRange(enrollments);
                await _context.SaveChangesAsync();

                // Delete user
                var result = await _userManager.DeleteAsync(student);
                
                if (result.Succeeded)
                {
                    SetSuccessMessage($"Student '{student.FullName}' and {enrollments.Count} enrollment(s) deleted successfully.");
                }
                else
                {
                    SetErrorMessage("Failed to delete student: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Cannot delete this student: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Helper to populate dropdowns.
        /// </summary>
        private async Task PopulateDropdowns()
        {
            ViewBag.Batches = new SelectList(
                await _context.Batches.OrderBy(b => b.Name).ToListAsync(),
                "Id",
                "Name"
            );

            ViewBag.Sections = new SelectList(
                await _context.Sections.OrderBy(s => s.Name).ToListAsync(),
                "Id",
                "Name"
            );
        }
    }
}
