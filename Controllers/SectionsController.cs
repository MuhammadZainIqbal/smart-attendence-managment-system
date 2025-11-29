using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Sections (e.g., CS-A, CS-B) for the current Institute.
    /// </summary>
    public class SectionsController : BaseAdminController
    {
        public SectionsController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Sections
        public async Task<IActionResult> Index()
        {
            // Global Query Filter automatically filters by CurrentInstituteId
            var sections = await _context.Sections
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(sections);
        }

        // GET: Sections/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Section section)
        {
            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");
            
            // Check for duplicates (case-insensitive, tenant-scoped)
            var duplicateExists = await _context.Sections
                .AnyAsync(s => s.Name.ToLower() == section.Name.ToLower());
            
            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A section with this name already exists in your institute.");
            }
            
            if (ModelState.IsValid)
            {
                // Automatically assign current Institute
                section.InstituteId = CurrentInstituteId;

                _context.Add(section);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Section '{section.Name}' created successfully.");
                return RedirectToAction(nameof(Index));
            }

            return View(section);
        }

        // GET: Sections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var section = await _context.Sections.FindAsync(id);
            
            if (section == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(section.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            return View(section);
        }

        // POST: Sections/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Section section)
        {
            if (id != section.Id)
                return NotFound();

            // Load the existing section from database to verify ownership
            var existingSection = await _context.Sections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            
            if (existingSection == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(existingSection.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");

            // Check for duplicates (case-insensitive, tenant-scoped, exclude current entity)
            var duplicateExists = await _context.Sections
                .AnyAsync(s => s.Id != id && s.Name.ToLower() == section.Name.ToLower());
            
            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A section with this name already exists in your institute.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure InstituteId cannot be changed
                    section.InstituteId = existingSection.InstituteId;
                    
                    _context.Update(section);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Section '{section.Name}' updated successfully.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SectionExists(section.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(section);
        }

        // POST: Sections/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.Sections.FindAsync(id);
            
            if (section == null)
            {
                SetErrorMessage("Section not found.");
                return RedirectToAction(nameof(Index));
            }

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(section.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Section '{section.Name}' deleted successfully.");
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("Cannot delete this section because it has related records (Students, Course Offerings, etc.).");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SectionExists(int id)
        {
            return _context.Sections.Any(e => e.Id == id);
        }
    }
}
