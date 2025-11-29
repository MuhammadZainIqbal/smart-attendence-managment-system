using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Teachers for the current Institute.
    /// Creates teachers with random passwords and sends welcome emails.
    /// </summary>
    public class TeachersController : BaseAdminController
    {
        private readonly IEmailSender _emailSender;

        public TeachersController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IEmailSender emailSender)
            : base(userManager, context)
        {
            _emailSender = emailSender;
        }

        // GET: Teachers
        public async Task<IActionResult> Index()
        {
            // Get all users in current institute
            var allUsers = await _userManager.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Filter to only teachers
            var teachers = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Teacher"))
                {
                    teachers.Add(user);
                }
            }

            return View(teachers);
        }

        // GET: Teachers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teachers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fullName, string email)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Full Name and Email are required.");
                return View();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("email", "This email is already registered.");
                return View();
            }

            try
            {
                // Generate random password
                string randomPassword = PasswordGenerator.Generate(12);

                // Create new teacher user
                var teacher = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    InstituteId = CurrentInstituteId,
                    EmailConfirmed = true, // Auto-confirm for admin-created users
                    IsPasswordChanged = false // Force password change on first login
                };

                var result = await _userManager.CreateAsync(teacher, randomPassword);

                if (result.Succeeded)
                {
                    // Assign Teacher role
                    await _userManager.AddToRoleAsync(teacher, "Teacher");

                    // Get Institute details for email
                    var institute = await _context.Institutes
                        .FirstOrDefaultAsync(i => i.Id == CurrentInstituteId);

                    // Send welcome email with credentials
                    try
                    {
                        string emailSubject = $"Welcome to {institute?.Name} - Teacher Account Created";
                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #28a745;'>Welcome to Attendance Management System!</h2>
                                <p>Hello <strong>{teacher.FullName}</strong>,</p>
                                <p>Your Teacher account has been created for <strong>{institute?.Name}</strong>.</p>
                                
                                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                    <h3 style='margin-top: 0; color: #333;'>Your Login Credentials</h3>
                                    <p><strong>Institute Code:</strong> <span style='color: #ffc107; font-size: 1.2em;'>{institute?.Code}</span></p>
                                    <p><strong>Email:</strong> {email}</p>
                                    <p><strong>Temporary Password:</strong> <span style='color: #007bff; font-size: 1.2em; font-family: monospace;'>{randomPassword}</span></p>
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
                        
                        SetSuccessMessage($"Teacher '{fullName}' created successfully. Welcome email sent to {email}.");
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail the creation
                        Console.WriteLine($"Failed to send welcome email: {emailEx.Message}");
                        SetSuccessMessage($"Teacher '{fullName}' created successfully. WARNING: Email delivery failed. Password: {randomPassword}");
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

            return View();
        }

        // POST: Teachers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetErrorMessage("Invalid teacher ID.");
                return RedirectToAction(nameof(Index));
            }

            var teacher = await _userManager.FindByIdAsync(id);
            
            if (teacher == null)
            {
                SetErrorMessage("Teacher not found.");
                return RedirectToAction(nameof(Index));
            }

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(teacher.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _userManager.DeleteAsync(teacher);
                
                if (result.Succeeded)
                {
                    SetSuccessMessage($"Teacher '{teacher.FullName}' deleted successfully.");
                }
                else
                {
                    SetErrorMessage("Failed to delete teacher: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Cannot delete this teacher: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
