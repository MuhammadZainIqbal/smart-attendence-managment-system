using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// Manages Class Schedules (Time-Locker for CourseOfferings).
    /// Defines when a course meets (day, start time, end time, grace period).
    /// </summary>
    public class ClassSchedulesController : BaseAdminController
    {
        public ClassSchedulesController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
            : base(userManager, context)
        {
        }

        // GET: ClassSchedules
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
            var query = _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .AsQueryable();

            // Apply filters (AND logic)
            if (batchId.HasValue)
            {
                query = query.Where(cs => cs.CourseOffering.BatchId == batchId.Value);
            }

            if (sectionId.HasValue)
            {
                query = query.Where(cs => cs.CourseOffering.SectionId == sectionId.Value);
            }

            var schedules = await query
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();

            return View(schedules);
        }

        // GET: ClassSchedules/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: ClassSchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int courseOfferingId,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int gracePeriodMinutes = 15)
        {
            if (courseOfferingId == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a course offering.");
                await PopulateDropdowns();
                return View();
            }

            // Validate times
            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after start time.");
                await PopulateDropdowns();
                return View();
            }

            // Verify course offering belongs to current institute
            var courseOffering = await _context.CourseOfferings
                .FirstOrDefaultAsync(co => co.Id == courseOfferingId);

            if (courseOffering == null || !BelongsToCurrentInstitute(courseOffering.InstituteId))
            {
                ModelState.AddModelError(string.Empty, "Invalid course offering selected.");
                await PopulateDropdowns();
                return View();
            }

            // Check for duplicates (same course offering, same day, overlapping time)
            var hasConflict = await _context.ClassSchedules
                .AnyAsync(cs =>
                    cs.CourseOfferingId == courseOfferingId &&
                    cs.DayOfWeek == (DayOfWeek)dayOfWeek &&
                    (
                        (startTime >= cs.StartTime && startTime < cs.EndTime) ||
                        (endTime > cs.StartTime && endTime <= cs.EndTime) ||
                        (startTime <= cs.StartTime && endTime >= cs.EndTime)
                    ));

            if (hasConflict)
            {
                ModelState.AddModelError(string.Empty,
                    "This course offering already has a schedule on this day that overlaps with the specified time.");
                await PopulateDropdowns();
                return View();
            }

            try
            {
                var schedule = new ClassSchedule
                {
                    CourseOfferingId = courseOfferingId,
                    DayOfWeek = (DayOfWeek)dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    GracePeriodMinutes = gracePeriodMinutes,
                    InstituteId = CurrentInstituteId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ClassSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                SetSuccessMessage($"Class schedule created successfully for {((DayOfWeek)dayOfWeek).ToString()} at {startTime:hh\\:mm} - {endTime:hh\\:mm}.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error creating class schedule: {ex.Message}");
                await PopulateDropdowns();
                return View();
            }
        }

        // GET: ClassSchedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (schedule == null || !BelongsToCurrentInstitute(schedule.CourseOffering.InstituteId))
            {
                return NotFound();
            }

            // Populate dropdown for Days of Week
            ViewBag.DaysOfWeek = new SelectList(
                Enum.GetValues(typeof(DayOfWeek))
                    .Cast<DayOfWeek>()
                    .Select(d => new
                    {
                        Value = (int)d,
                        Text = d.ToString()
                    }),
                "Value",
                "Text",
                (int)schedule.DayOfWeek
            );

            return View(schedule);
        }

        // POST: ClassSchedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime,
            int gracePeriodMinutes)
        {
            var schedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (schedule == null || !BelongsToCurrentInstitute(schedule.CourseOffering.InstituteId))
            {
                return NotFound();
            }

            // Validate times
            if (endTime <= startTime)
            {
                ModelState.AddModelError(string.Empty, "End time must be after start time.");
                
                // Re-populate dropdown
                ViewBag.DaysOfWeek = new SelectList(
                    Enum.GetValues(typeof(DayOfWeek))
                        .Cast<DayOfWeek>()
                        .Select(d => new
                        {
                            Value = (int)d,
                            Text = d.ToString()
                        }),
                    "Value",
                    "Text",
                    dayOfWeek
                );
                
                return View(schedule);
            }

            // Check for time conflicts (same CourseOffering, same day, overlapping time)
            // EXCLUDE the current schedule from the conflict check
            var hasConflict = await _context.ClassSchedules
                .AnyAsync(cs =>
                    cs.Id != id && // Exclude current schedule
                    cs.CourseOfferingId == schedule.CourseOfferingId &&
                    cs.DayOfWeek == (DayOfWeek)dayOfWeek &&
                    (
                        (startTime >= cs.StartTime && startTime < cs.EndTime) ||
                        (endTime > cs.StartTime && endTime <= cs.EndTime) ||
                        (startTime <= cs.StartTime && endTime >= cs.EndTime)
                    ));

            if (hasConflict)
            {
                ModelState.AddModelError(string.Empty,
                    "This course offering already has a schedule on this day that overlaps with the specified time.");
                
                // Re-populate dropdown
                ViewBag.DaysOfWeek = new SelectList(
                    Enum.GetValues(typeof(DayOfWeek))
                        .Cast<DayOfWeek>()
                        .Select(d => new
                        {
                            Value = (int)d,
                            Text = d.ToString()
                        }),
                    "Value",
                    "Text",
                    dayOfWeek
                );
                
                return View(schedule);
            }

            try
            {
                // Update the schedule properties
                schedule.DayOfWeek = (DayOfWeek)dayOfWeek;
                schedule.StartTime = startTime;
                schedule.EndTime = endTime;
                schedule.GracePeriodMinutes = gracePeriodMinutes;

                await _context.SaveChangesAsync();

                SetSuccessMessage($"Class schedule updated successfully for {((DayOfWeek)dayOfWeek).ToString()} at {startTime:hh\\:mm} - {endTime:hh\\:mm}.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error updating class schedule: {ex.Message}");
                
                // Re-populate dropdown
                ViewBag.DaysOfWeek = new SelectList(
                    Enum.GetValues(typeof(DayOfWeek))
                        .Cast<DayOfWeek>()
                        .Select(d => new
                        {
                            Value = (int)d,
                            Text = d.ToString()
                        }),
                    "Value",
                    "Text",
                    dayOfWeek
                );
                
                return View(schedule);
            }
        }

        // GET: ClassSchedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.ClassSchedules
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Batch)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Section)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Subject)
                .Include(cs => cs.CourseOffering)
                    .ThenInclude(co => co.Teacher)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (schedule == null || !BelongsToCurrentInstitute(schedule.CourseOffering.InstituteId))
            {
                return NotFound();
            }

            return View(schedule);
        }

        // POST: ClassSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Use IgnoreQueryFilters to find the schedule even if already soft-deleted
            var schedule = await _context.ClassSchedules
                .IgnoreQueryFilters()
                .Include(cs => cs.CourseOffering)
                .FirstOrDefaultAsync(cs => cs.Id == id && cs.InstituteId == CurrentInstituteId);

            if (schedule == null || !BelongsToCurrentInstitute(schedule.CourseOffering.InstituteId))
            {
                return NotFound();
            }

            // Check if already soft-deleted
            if (schedule.IsDeleted)
            {
                SetErrorMessage("This class schedule has already been archived.");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // SOFT DELETE: Set IsDeleted flag instead of removing
                schedule.IsDeleted = true;
                await _context.SaveChangesAsync();

                SetSuccessMessage("Class schedule archived successfully. Attendance history is preserved.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error archiving class schedule: {ex.Message}");
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        /// <summary>
        /// Populates dropdowns for Create form.
        /// CourseOfferings are filtered by CurrentInstituteId.
        /// </summary>
        private async Task PopulateDropdowns()
        {
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

            // Day of Week dropdown
            ViewBag.DaysOfWeek = new SelectList(
                Enum.GetValues(typeof(DayOfWeek))
                    .Cast<DayOfWeek>()
                    .Select(d => new
                    {
                        Value = (int)d,
                        Text = d.ToString()
                    }),
                "Value",
                "Text"
            );
        }
    }
}
