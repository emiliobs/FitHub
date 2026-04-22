using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

// Requires authentication by default for all actions
[Authorize]
public class FitnessClassesController : Controller
{
    private readonly ApplicationDbContext _context;

    // Inject the database context
    public FitnessClassesController(ApplicationDbContext context)
    {
        this._context = context;
    }

    // GET: Show all fitness classes and active seat counts
    [AllowAnonymous] // Publicly visible to guests
    public async Task<IActionResult> Index()
    {
        // Fetch all classes with related data
        var classes = await _context.FitnessClasses
            .Include(f => f.Category)
            .Include(f => f.Instructor)
            .OrderByDescending(f => f.ScheduleDate)
            .ToListAsync();

        // Calculate active bookings per class
        var bookingCounts = await _context.Bookings
            .Where(b => b.Status == BookingStatus.Active)
            .GroupBy(b => b.FitnessClassId)
            .Select(g => new { FitnessClassId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Assign active counts to the view model
        foreach (var fitnessClass in classes)
        {
            fitnessClass.ActiveBookingsCount = bookingCounts
                .FirstOrDefault(x => x.FitnessClassId == fitnessClass.Id)?.Count ?? 0;
        }

        return View(classes);
    }

    // GET: Show create form with dropdowns
    [Authorize(Policy = "CanManageClasses")] // Restricted access
    public async Task<IActionResult> Create()
    {
        var viewModel = new FitnessClassViewModel
        {
            // Populate dropdown lists for UI
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

    // POST: Save new fitness class to database
    [HttpPost]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FitnessClassViewModel fitnessClassViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Map view model data to domain model for database storage
                var newClass = new FitnessClass
                {
                    Title = fitnessClassViewModel.Title,
                    Description = fitnessClassViewModel.Description,
                    Capacity = fitnessClassViewModel.Capacity,
                    // Convert local browser time to UTC for secure database storage
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

        // Reload dropdowns if validation fails
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

    // GET: Show edit form with existing data
    [HttpGet]
    [Authorize(Policy = "CanManageClasses")]
    public async Task<IActionResult> Edit(int id)
    {
        // Load the existing class data for editing
        var fitnessClass = await _context.FitnessClasses.FindAsync(id);

        if (fitnessClass is null)
        {
            return NotFound();
        }

        // Map existing class data to the view model for editing
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

        // Pass the view model to the edit view
        return View(viewModel);
    }

    // POST: Update existing fitness class
    [HttpPost]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FitnessClassViewModel fitnessClassViewModel)
    {
        if (id != fitnessClassViewModel.Id)
        {
            return NotFound();
        }

        // Validate the incoming data before attempting to update the database
        if (ModelState.IsValid)
        {
            try
            {
                // Load the existing class from the database to update its properties
                var fitnessClass = await _context.FitnessClasses.FindAsync(id);

                if (fitnessClass is null)
                {
                    return NotFound();
                }

                // Update properties
                fitnessClass.Title = fitnessClassViewModel.Title;
                fitnessClass.Description = fitnessClassViewModel.Description;
                fitnessClass.Capacity = fitnessClassViewModel.Capacity;
                fitnessClass.ScheduleDate = ConvertBrowserLocalToUtc(fitnessClassViewModel.ScheduleDate, fitnessClassViewModel.BrowserTimeZone, fitnessClassViewModel.BrowserUtcOffsetMinutes);
                fitnessClass.Price = fitnessClassViewModel.Price;
                fitnessClass.CategoryId = fitnessClassViewModel.CategoryId;
                fitnessClass.InstructorId = fitnessClassViewModel.InstructorId;

                // Mark the entity as modified and save changes to the database
                _context.FitnessClasses.Update(fitnessClass);

                // Save changes to the database with error handling
                await _context.SaveChangesAsync();

                TempData["Success"] = "Fitness session updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while updating the fitness class: {ex.Message}";
            }
        }

        // Reload dropdowns if validation fails
        return View(fitnessClassViewModel);
    }

    // POST: Securely delete a fitness class
    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = "CanManageClasses")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            // Load the class along with its bookings to check for active reservations before deletion
            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.Id == id);

            // If the class doesn't exist, show an error message and redirect to the index page
            if (fitnessClass is null)
            {
                TempData["Error"] = "The class you are trying to delete was not found.";
                return RedirectToAction(nameof(Index));
            }

            // Block deletion if members are already enrolled
            if (fitnessClass.Bookings.Any())
            {
                TempData["Error"] = $"Action Denied: You Cannot delete {fitnessClass.Title} because it has {fitnessClass.Bookings.Count} Active reservations. Please cancel the booking first.";
                return RedirectToAction(nameof(Index));
            }

            // If no active bookings, proceed with deletion
            _context.FitnessClasses.Remove(fitnessClass);

            // Save changes to the database with error handling
            await _context.SaveChangesAsync();

            TempData["Success"] = $"The training session {fitnessClass.Title} has been deleted successfully from the schedule!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"A server error occurred while trying delete the fitness class: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Show list of users booked for a specific class
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
            // Load class details along with enrolled participants
            var fitnessClass = await _context.FitnessClasses
                .Include(c => c.Instructor)// Include instructor details for better context in the attendance view
                .Include(c => c.Category)// Include category details for better context in the attendance view
                .Include(c => c.Bookings)// Include bookings to get the list of attendees
                .ThenInclude(b => b.ApplicationUser)// Include user details for each booking to display attendee information
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

    // Helper: Converts client-side local time to standard UTC for database
    private static DateTime ConvertBrowserLocalToUtc(DateTime browserLocalDateTime, string? browserTimeZone, int browserUtcOffsetMinutes)
    {
        var unspecifiedLocal = DateTime.SpecifyKind(browserLocalDateTime, DateTimeKind.Unspecified);

        if (!string.IsNullOrWhiteSpace(browserTimeZone))
        {
            try
            {
                // Attempt to convert using the browser's timezone ID for more accurate conversion
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(browserTimeZone);

                // Convert the unspecified local time to UTC using the identified timezone
                return TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, timeZone);
            }
            catch
            {
                // Fall back to browser offset if the OS timezone ID can't be resolved.
            }
        }

        // If timezone ID is unavailable or invalid, use the provided UTC offset to convert to UTC
        var offset = TimeSpan.FromMinutes(-browserUtcOffsetMinutes);

        // Create a DateTimeOffset with the local time and offset, then convert to UTC
        var dto = new DateTimeOffset(unspecifiedLocal, offset);

        // Return the UTC DateTime for storage in the database
        return dto.UtcDateTime;
    }
}