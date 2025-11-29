using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Admin Dashboard Controller.
    /// Shows summary statistics and provides navigation to management features.
    /// </summary>
    public class AdminController : BaseAdminController
    {
        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        /// <summary>
        /// Admin Dashboard Homepage with summary cards.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Get Institute details
            var institute = await _context.Institutes
                .FirstOrDefaultAsync(i => i.Id == CurrentInstituteId);

            if (institute == null)
                return RedirectToAction("Login", "Account");

            // Get summary counts (Global Query Filter automatically applies)
            var batchCount = await _context.Batches.CountAsync();
            var sectionCount = await _context.Sections.CountAsync();
            var subjectCount = await _context.Subjects.CountAsync();
            
            // Count users by role
            var allUsers = await _userManager.Users
                .Where(u => u.InstituteId == CurrentInstituteId)
                .ToListAsync();

            var teacherCount = 0;
            var studentCount = 0;

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Teacher")) teacherCount++;
                if (roles.Contains("Student")) studentCount++;
            }

            // Count active courses (CourseOfferings)
            var courseOfferingCount = await _context.CourseOfferings.CountAsync();

            // Pass data to view
            ViewBag.InstituteName = institute.Name;
            ViewBag.InstituteCode = institute.Code;
            ViewBag.BatchCount = batchCount;
            ViewBag.SectionCount = sectionCount;
            ViewBag.SubjectCount = subjectCount;
            ViewBag.TeacherCount = teacherCount;
            ViewBag.StudentCount = studentCount;
            ViewBag.CourseOfferingCount = courseOfferingCount;

            return View();
        }
    }
}
