using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

[Authorize]
public class FitnessClassesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FitnessClassesController(ApplicationDbContext context)
    {
        this._context = context;
    }

    // GET: FitnessClasses
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var classes = await _context.FitnessClasses
            .Include(f => f.Category)
            .Include(f => f.Instructor)
            .OrderByDescending(f => f.ScheduleDate)
            .ToListAsync();

        var bookingCounts = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Active)
            .GroupBy(b => b.FitnessClassId)
            .Select(g => new { FitnessClassId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var fitnessClass in classes)
        {
            fitnessClass.ActiveBookingsCount = bookingCounts
                .FirstOrDefault(x => x.FitnessClassId == fitnessClass.Id)?.Count ?? 0;
        }

        return View(classes);
    }

    [Authorize(Policy = "CanManageClasses")]
    public async Task<IActionResult> Create()
    {
        var viewModel = new FitnessClassViewModel
        {
            Categories = await _context.Categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync(),

            Instructors = await _context.Instructors.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            }).ToListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FitnessClassViewModel fitnessClassViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var newClass = new FitnessClass
                {
                    Title = fitnessClassViewModel.Title,
                    Description = fitnessClassViewModel.Description,
                    Capacity = fitnessClassViewModel.Capacity,
                    ScheduleDate = ConvertBrowserLocalToUtc(fitnessClassViewModel.ScheduleDate, fitnessClassViewModel.BrowserTimeZone, fitnessClassViewModel.BrowserUtcOffsetMinutes),
                    Price = fitnessClassViewModel.Price,
                    CategoryId = fitnessClassViewModel.CategoryId,
                    InstructorId = fitnessClassViewModel.InstructorId
                };

                _context.FitnessClasses.Add(newClass);
                await _context.SaveChangesAsync();

                TempData["Success"] = "New Fitness class created successfully!";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred while creating the fitness class: {ex.Message}";
        }

        fitnessClassViewModel.Categories = await _context.Categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        }).ToListAsync();

        fitnessClassViewModel.Instructors = await _context.Instructors.Select(i => new SelectListItem
        {
            Value = i.Id.ToString(),
            Text = i.Name
        }).ToListAsync();

        return View(fitnessClassViewModel);
    }

    [HttpGet]
    [Authorize(Policy = "CanManageClasses")]
    public async Task<IActionResult> Edit(int id)
    {
        var fitnessClass = await _context.FitnessClasses.FindAsync(id);

        if (fitnessClass is null)
        {
            return NotFound();
        }

        var viewModel = new FitnessClassViewModel
        {
            Id = fitnessClass.Id,
            Title = fitnessClass.Title,
            Description = fitnessClass.Description,
            Capacity = fitnessClass.Capacity,
            ScheduleDate = fitnessClass.ScheduleDate,
            Price = fitnessClass.Price,
            CategoryId = fitnessClass.CategoryId,
            InstructorId = fitnessClass.InstructorId,
            Categories = await _context.Categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToListAsync(),
            Instructors = await _context.Instructors.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            }).ToListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FitnessClassViewModel fitnessClassViewModel)
    {
        if (id != fitnessClassViewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var fitnessClass = await _context.FitnessClasses.FindAsync(id);

                if (fitnessClass is null)
                {
                    return NotFound();
                }

                fitnessClass.Title = fitnessClassViewModel.Title;
                fitnessClass.Description = fitnessClassViewModel.Description;
                fitnessClass.Capacity = fitnessClassViewModel.Capacity;
                fitnessClass.ScheduleDate = ConvertBrowserLocalToUtc(fitnessClassViewModel.ScheduleDate, fitnessClassViewModel.BrowserTimeZone, fitnessClassViewModel.BrowserUtcOffsetMinutes);
                fitnessClass.Price = fitnessClassViewModel.Price;
                fitnessClass.CategoryId = fitnessClassViewModel.CategoryId;
                fitnessClass.InstructorId = fitnessClassViewModel.InstructorId;

                _context.FitnessClasses.Update(fitnessClass);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Fitness session updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while updating the fitness class: {ex.Message}";
            }
        }

        return View(fitnessClassViewModel);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fitnessClass is null)
            {
                TempData["Error"] = "The class you are trying to delete was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (fitnessClass.Bookings.Any())
            {
                TempData["Error"] = $"Action Denied: You Cannot delete {fitnessClass.Title} because it has {fitnessClass.Bookings.Count} Active reservations. Please cancel the booking first.";
                return RedirectToAction(nameof(Index));
            }

            _context.FitnessClasses.Remove(fitnessClass);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"The training session {fitnessClass.Title} has been deleted successfully from the schedule!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"A server error occurred while trying delete the fitness class: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "CanManageClasses")]
    public async Task<IActionResult> Attendance(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        try
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Bookings)
                .ThenInclude(b => b.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fitnessClass is null)
            {
                return NotFound();
            }

            return View(fitnessClass);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred while retrieving attendance data: {ex.Message}";

            return RedirectToAction(nameof(Index));
        }
    }

    private static DateTime ConvertBrowserLocalToUtc(DateTime browserLocalDateTime, string? browserTimeZone, int browserUtcOffsetMinutes)
    {
        var unspecifiedLocal = DateTime.SpecifyKind(browserLocalDateTime, DateTimeKind.Unspecified);

        if (!string.IsNullOrWhiteSpace(browserTimeZone))
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(browserTimeZone);
                return TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, timeZone);
            }
            catch
            {
                // Fall back to browser offset if the OS timezone ID can't be resolved.
            }
        }

        var offset = TimeSpan.FromMinutes(-browserUtcOffsetMinutes);
        var dto = new DateTimeOffset(unspecifiedLocal, offset);
        return dto.UtcDateTime;
    }
}