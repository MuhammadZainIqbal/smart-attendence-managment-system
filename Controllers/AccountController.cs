using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using AttendenceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Handles Multi-Tenant Authentication: Sign Up, Login, Logout, and Password Management.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

        // ==========================================
        // SIGN UP (Institute Admin Registration)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // Use a transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // CRITICAL: Disable Global Query Filter during signup
                var originalInstituteId = _context.CurrentInstituteId;
                _context.CurrentInstituteId = null;

                // Generate unique Institute Code
                string instituteCode = await GenerateUniqueInstituteCode(model.InstituteName);

                // Create new Institute
                var institute = new Institute
                {
                    Name = model.InstituteName,
                    Code = instituteCode,
                    AdminEmail = model.Email,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Institutes.Add(institute);
                await _context.SaveChangesAsync();

                // Create Admin User with EmailConfirmed = FALSE
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    InstituteId = institute.Id,
                    IsPasswordChanged = true,
                    EmailConfirmed = false // MUST verify email via OTP
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(user, "Admin");

                    // Generate 6-digit OTP
                    string otp = GenerateOTP();

                    // Store OTP as authentication token (expires in 10 minutes)
                    await _userManager.SetAuthenticationTokenAsync(
                        user,
                        "EmailVerification",
                        "OTP",
                        otp);

                    // Commit transaction BEFORE sending email
                    await transaction.CommitAsync();

                    // Restore context
                    _context.CurrentInstituteId = originalInstituteId;

                    // Send OTP via Email (after commit, so no rollback needed)
                    bool emailSent = false;
                    try
                    {
                        string emailSubject = $"Verify Your Institute - {institute.Name}";
                        string emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #28a745;'>Welcome to Attendance Management System!</h2>
                                <p>Hello <strong>{user.FullName}</strong>,</p>
                                <p>Thank you for registering your institute <strong>{institute.Name}</strong>.</p>
                                <p>Your Institute Code is: <strong style='color: #ffc107; font-size: 1.5em;'>{instituteCode}</strong></p>
                                <hr>
                                <p>To complete your registration, please verify your email address using the OTP below:</p>
                                <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                                    <h1 style='color: #007bff; margin: 0; letter-spacing: 5px;'>{otp}</h1>
                                </div>
                                <p><strong>Note:</strong> This OTP is valid for 10 minutes.</p>
                                <p>If you did not create this account, please ignore this email.</p>
                                <br>
                                <p>Best regards,<br>AMS Team</p>
                            </div>";

                        await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);
                        emailSent = true;
                    }
                    catch (Exception emailEx)
                    {
                        // Log full exception details for debugging
                        Console.WriteLine($"============ EMAIL SENDING FAILED ============");
                        Console.WriteLine($"To: {model.Email}");
                        Console.WriteLine($"Error: {emailEx.Message}");
                        Console.WriteLine($"Stack Trace: {emailEx.StackTrace}");
                        if (emailEx.InnerException != null)
                        {
                            Console.WriteLine($"Inner Exception: {emailEx.InnerException.Message}");
                        }
                        Console.WriteLine($"OTP (for testing): {otp}");
                        Console.WriteLine($"==============================================");
                        
                        // Show warning to user
                        TempData["EmailWarning"] = "Email delivery failed. Please check the console for your OTP or contact support.";
                    }

                    // Redirect to OTP verification page
                    TempData["VerificationEmail"] = model.Email;
                    TempData["InstituteCode"] = instituteCode;
                    TempData["EmailSent"] = emailSent;
                    
                    return RedirectToAction("VerifyEmail", new { userId = user.Id });
                }

                // If user creation failed, rollback
                await transaction.RollbackAsync();
                _context.CurrentInstituteId = originalInstituteId;

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                // Try to rollback, but catch any errors if transaction already completed
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Transaction already completed, ignore rollback error
                }
                
                _context.CurrentInstituteId = null; // Reset context
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError(string.Empty, $"An error occurred during registration: {innerMessage}");
                return View(model);
            }
        }

        // ==========================================
        // LOGIN (Multi-Tenant Authentication)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Step 1: Verify Institute Code exists
            var institute = await _context.Institutes
                .FirstOrDefaultAsync(i => i.Code == model.InstituteCode);

            if (institute == null)
            {
                ModelState.AddModelError("InstituteCode", "Invalid Institute Code.");
                return View(model);
            }

            // Step 2: Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Step 3: CRITICAL SECURITY CHECK - Verify user belongs to this Institute
            if (user.InstituteId != institute.Id)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Step 4: Verify password and sign in
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName ?? user.Email ?? string.Empty,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Step 5: Check if password change is required
                if (!user.IsPasswordChanged)
                {
                    // Force password change for users created by Admin
                    return RedirectToAction(nameof(ChangePassword));
                }

                // Step 6: Set CurrentInstituteId in DbContext for Global Query Filter
                _context.CurrentInstituteId = user.InstituteId;

                // Step 7: Redirect based on role
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Admin");
                else if (roles.Contains("Teacher"))
                    return RedirectToAction("Index", "Teacher");
                else if (roles.Contains("Student"))
                    return RedirectToAction("Index", "Student");

                // Fallback
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account has been locked out.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // ==========================================
        // COMPLETE SIGNUP (Auto-login after seeing Institute Code)
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSignUp()
        {
            var email = TempData["PendingLoginEmail"]?.ToString();
            var password = TempData["PendingLoginPassword"]?.ToString();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                // Verify password before signing in
                var passwordValid = await _userManager.CheckPasswordAsync(user, password);
                
                if (passwordValid)
                {
                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    TempData["SuccessMessage"] = $"Welcome! Your institute has been registered successfully.";

                    // Redirect to Admin Dashboard
                    return RedirectToAction("Index", "Admin");
                }
            }

            return RedirectToAction("Login");
        }

        // ==========================================
        // LOGOUT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ==========================================
        // CHANGE PASSWORD (Force Change for Created Users)
        // ==========================================

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Update IsPasswordChanged flag
                user.IsPasswordChanged = true;
                await _userManager.UpdateAsync(user);

                // Sign in again to refresh the security stamp
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] = "Your password has been changed successfully.";

                // Redirect based on role
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Admin");
                else if (roles.Contains("Teacher"))
                    return RedirectToAction("Index", "Teacher");
                else if (roles.Contains("Student"))
                    return RedirectToAction("Index", "Student");

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==========================================
        // VERIFY EMAIL (OTP Verification after Sign Up)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            var model = new VerifyOtpViewModel
            {
                UserId = userId,
                Email = TempData["VerificationEmail"]?.ToString() ?? ""
            };

            ViewBag.InstituteCode = TempData["InstituteCode"]?.ToString();

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification request.");
                return View(model);
            }

            // Retrieve stored OTP
            var storedOtp = await _userManager.GetAuthenticationTokenAsync(user, "EmailVerification", "OTP");

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != model.Code)
            {
                ModelState.AddModelError("Code", "Invalid or expired OTP. Please request a new one.");
                return View(model);
            }

            // OTP is valid - confirm email
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Remove the used OTP
            await _userManager.RemoveAuthenticationTokenAsync(user, "EmailVerification", "OTP");

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = "Email verified successfully! Welcome to your dashboard.";

            // Redirect to Admin Dashboard
            return RedirectToAction("Index", "Admin");
        }

        // ==========================================
        // FORGOT PASSWORD Flow
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.EmailConfirmed)
            {
                // Don't reveal that the user doesn't exist or is not confirmed
                TempData["InfoMessage"] = "If your email is registered, you will receive an OTP shortly.";
                return View(model);
            }

            // Generate 6-digit OTP
            string otp = GenerateOTP();

            // Store OTP
            await _userManager.SetAuthenticationTokenAsync(user, "PasswordReset", "OTP", otp);

            // Send OTP via Email
            try
            {
                string emailSubject = "Password Reset OTP";
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #dc3545;'>Password Reset Request</h2>
                        <p>Hello <strong>{user.FullName}</strong>,</p>
                        <p>We received a request to reset your password. Use the OTP below to proceed:</p>
                        <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                            <h1 style='color: #007bff; margin: 0; letter-spacing: 5px;'>{otp}</h1>
                        </div>
                        <p><strong>Note:</strong> This OTP is valid for 10 minutes.</p>
                        <p>If you did not request a password reset, please ignore this email.</p>
                        <br>
                        <p>Best regards,<br>AMS Team</p>
                    </div>";

                await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);
            }
            catch (Exception emailEx)
            {
                // Log error but proceed (OTP is already stored in database)
                Console.WriteLine($"Password reset email failed: {emailEx.Message}");
            }

            // Redirect to OTP verification
            TempData["ResetEmail"] = model.Email;
            return RedirectToAction("VerifyResetOtp");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyResetOtp()
        {
            Console.WriteLine("========== VerifyResetOtp GET ==========");
            var email = TempData["ResetEmail"]?.ToString() ?? "";
            Console.WriteLine($"Email from TempData: {email}");
            
            var model = new VerifyOtpViewModel
            {
                Email = email
            };

            if (string.IsNullOrEmpty(model.Email))
            {
                Console.WriteLine("Email is empty - redirecting to ForgotPassword");
                return RedirectToAction("ForgotPassword");
            }

            // Keep TempData for POST
            TempData.Keep("ResetEmail");
            Console.WriteLine($"Model.Email set to: {model.Email}");
            Console.WriteLine($"=======================================");
            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetOtp(VerifyOtpViewModel model)
        {
            Console.WriteLine($"========== VerifyResetOtp POST ==========");
            Console.WriteLine($"Email: {model.Email}");
            Console.WriteLine($"Code: {model.Code}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState Invalid - Errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"  - {error.ErrorMessage}");
                }
                // Keep the email in model for redisplay
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                Console.WriteLine("Email is empty - redirecting to ForgotPassword");
                ModelState.AddModelError(string.Empty, "Session expired. Please try again.");
                return RedirectToAction("ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                Console.WriteLine($"User not found with email: {model.Email}");
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return View(model);
            }

            // Verify OTP
            var storedOtp = await _userManager.GetAuthenticationTokenAsync(user, "PasswordReset", "OTP");
            Console.WriteLine($"Stored OTP: {storedOtp}");
            Console.WriteLine($"User entered OTP: {model.Code}");
            Console.WriteLine($"OTP Match: {storedOtp == model.Code}");

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != model.Code)
            {
                Console.WriteLine("OTP validation failed");
                ModelState.AddModelError("Code", "Invalid or expired OTP.");
                return View(model);
            }

            // OTP valid - generate password reset token
            Console.WriteLine("OTP valid - generating reset token");
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Remove the used OTP
            await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordReset", "OTP");

            // Redirect to reset password page
            TempData["ResetToken"] = resetToken;
            TempData["ResetEmail"] = model.Email;

            Console.WriteLine("Redirecting to ResetPassword");
            Console.WriteLine($"=========================================");
            
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            var model = new ResetPasswordViewModel
            {
                Email = TempData["ResetEmail"]?.ToString() ?? "",
                Token = TempData["ResetToken"]?.ToString() ?? ""
            };

            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token))
                return RedirectToAction("ForgotPassword");

            TempData.Keep("ResetEmail");
            TempData.Keep("ResetToken");

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid request.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password reset successfully. Please login with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            TempData.Keep("ResetEmail");
            TempData.Keep("ResetToken");

            return View(model);
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Generates a cryptographically secure 6-digit OTP.
        /// </summary>
        private string GenerateOTP()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);
                int value = Math.Abs(BitConverter.ToInt32(randomNumber, 0));
                return (value % 1000000).ToString("D6"); // 6-digit OTP
            }
        }

        /// <summary>
        /// Generates a unique Institute Code: First 3 letters (uppercase) + dash + 4 random digits.
        /// Example: "Punjab University" -> "PUN-4821"
        /// </summary>
        private async Task<string> GenerateUniqueInstituteCode(string instituteName)
        {
            // Extract first 3 letters (or pad with 'X' if shorter)
            string prefix = new string(instituteName
                .Where(char.IsLetter)
                .Take(3)
                .ToArray())
                .ToUpper()
                .PadRight(3, 'X');

            string code;
            bool exists;

            do
            {
                // Generate 4 random digits
                var random = new Random();
                int digits = random.Next(1000, 9999);
                code = $"{prefix}-{digits}";

                // Check if code already exists (ignore global filter for Institutes table)
                exists = await _context.Institutes
                    .IgnoreQueryFilters()
                    .AnyAsync(i => i.Code == code);
            }
            while (exists);

            return code;
        }

        /// <summary>
        /// Redirects to local URL if valid, otherwise to Home.
        /// </summary>
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // TEST EMAIL (FOR DEBUGGING ONLY - REMOVE IN PRODUCTION)
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestEmail()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter an email address";
                return View();
            }

            try
            {
                string testOTP = GenerateOTP();
                string subject = "Test Email from AMS";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #28a745;'>Email Test Successful!</h2>
                        <p>If you received this email, your SMTP settings are working correctly.</p>
                        <p>Test OTP: <strong style='font-size: 1.5em; color: #007bff;'>{testOTP}</strong></p>
                        <hr>
                        <small>Current Time: {DateTime.Now}</small>
                    </div>";

                await _emailSender.SendEmailAsync(email, subject, body);
                
                ViewBag.Success = $"Test email sent successfully to {email}! Check your inbox. OTP: {testOTP}";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Failed to send email: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ViewBag.Error += $"\nInner Exception: {ex.InnerException.Message}";
                }
            }

            return View();
        }
    }
}
