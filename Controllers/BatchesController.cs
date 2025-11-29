using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Batches (e.g., Fall 2023, Spring 2024) for the current Institute.
    /// </summary>
    public class BatchesController : BaseAdminController
    {
        public BatchesController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: Batches
        public async Task<IActionResult> Index()
        {
            // Global Query Filter automatically filters by CurrentInstituteId
            var batches = await _context.Batches
                .OrderBy(b => b.Name)
                .ToListAsync();

            return View(batches);
        }

        // GET: Batches/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Batches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Batch batch)
        {
            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");
            
            // Check for duplicates (case-insensitive, tenant-scoped)
            var duplicateExists = await _context.Batches
                .AnyAsync(b => b.Name.ToLower() == batch.Name.ToLower());
            
            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A batch with this name already exists in your institute.");
            }
            
            if (ModelState.IsValid)
            {
                // Automatically assign current Institute
                batch.InstituteId = CurrentInstituteId;

                _context.Add(batch);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Batch '{batch.Name}' created successfully.");
                return RedirectToAction(nameof(Index));
            }

            return View(batch);
        }

        // GET: Batches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var batch = await _context.Batches.FindAsync(id);
            
            if (batch == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(batch.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            return View(batch);
        }

        // POST: Batches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Batch batch)
        {
            if (id != batch.Id)
                return NotFound();

            // Load the existing batch from database to verify ownership
            var existingBatch = await _context.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            
            if (existingBatch == null)
                return NotFound();

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(existingBatch.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            // Remove validation errors for properties we set automatically
            ModelState.Remove("InstituteId");
            ModelState.Remove("Institute");

            // Check for duplicates (case-insensitive, tenant-scoped, exclude current entity)
            var duplicateExists = await _context.Batches
                .AnyAsync(b => b.Id != id && b.Name.ToLower() == batch.Name.ToLower());
            
            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A batch with this name already exists in your institute.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure InstituteId cannot be changed
                    batch.InstituteId = existingBatch.InstituteId;
                    
                    _context.Update(batch);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage($"Batch '{batch.Name}' updated successfully.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BatchExists(batch.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(batch);
        }

        // POST: Batches/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var batch = await _context.Batches.FindAsync(id);
            
            if (batch == null)
            {
                SetErrorMessage("Batch not found.");
                return RedirectToAction(nameof(Index));
            }

            // Security: Verify ownership
            if (!BelongsToCurrentInstitute(batch.InstituteId))
            {
                SetErrorMessage("Unauthorized access.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Batches.Remove(batch);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Batch '{batch.Name}' deleted successfully.");
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("Cannot delete this batch because it has related records (Sections, Students, etc.).");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BatchExists(int id)
        {
            return _context.Batches.Any(e => e.Id == id);
        }
    }
}
