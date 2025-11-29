using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Subjects (e.g., Calculus, Physics) for the current Institute.
    /// </summary>
    public class SubjectsController : BaseAdminController
    {
        public SubjectsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            // Global Query Filter automatically filters by CurrentInstituteId
            var subjects = await _context.Subjects
                .OrderBy(s => s.Code)
                .ToListAsync();

            return View(subjects);
        }

        // GET: Subjects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Code")] Subject subject)
        {
            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");
            
            // Check for duplicate subject codes (case-insensitive, tenant-scoped)
            var duplicateCodeExists = await _context.Subjects
                .AnyAsync(s => s.Code.ToLower() == subject.Code.ToLower());
            
            if (duplicateCodeExists)
            {
                ModelState.AddModelError("Code", "A subject with this code already exists in your institute.");
            }
            
            if (ModelState.IsValid)
            {
                // Automatically assign current Institute
                subject.InstituteId = CurrentInstituteId;

                _context.Add(subject);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Subject '{subject.Name}' ({subject.Code}) created successfully.");
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        // GET: Subjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var subject = await _context.Subjects.FindAsync(id);
            
            if (subject == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(subject.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code")] Subject subject)
        {
            if (id != subject.Id)
                return NotFound();

            // Load the existing subject from database to verify ownership
            var existingSubject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            
            if (existingSubject == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(existingSubject.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");

            // Check for duplicate subject codes (case-insensitive, tenant-scoped, exclude current entity)
            var duplicateCodeExists = await _context.Subjects
                .AnyAsync(s => s.Id != id && s.Code.ToLower() == subject.Code.ToLower());
            
            if (duplicateCodeExists)
            {
                ModelState.AddModelError("Code", "A subject with this code already exists in your institute.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure InstituteId cannot be changed
                    subject.InstituteId = existingSubject.InstituteId;
                    
                    _context.Update(subject);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Subject '{subject.Name}' updated successfully.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubjectExists(subject.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        // POST: Subjects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            
            if (subject == null)
            {
                SetErrorMessage("Subject not found.");
                return RedirectToAction(nameof(Index));
            }

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(subject.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Subject '{subject.Name}' deleted successfully.");
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("Cannot delete this subject because it has related records (Course Offerings, etc.).");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
        }
    }
}
