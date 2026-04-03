using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

[Authorize(Roles = "Admin, Manager")] // Only Admins and Managers can manage fitness classes
public class FitnessClassesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FitnessClassesController(ApplicationDbContext context)
    {
        this._context = context;
    }

    // GET: FitnessClasses
    public async Task<IActionResult> Index()
    {
        // We include Category and Instructor to show their names in the list view
        var clasees = await _context.FitnessClasses
            .Include(f => f.Category)
            .Include(f => f.Instructor)
            .OrderByDescending(f => f.ScheduleDate)
            .ToListAsync();

        return View(clasees);
    }

    // GET: FitnessClasses/Details/5
    public async Task<IActionResult> Create()
    {
        var viewModel = new FitnessClassViewModel
        {
            // Populating Dropdowns
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

    // POST: FitnessClasses/Create
    [HttpPost]
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
                    ScheduleDate = fitnessClassViewModel.ScheduleDate,
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

        // If validation fails, we must reload the dropdowns before returning the view
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

    // GET: FitnessClasses/Edit/5
    [HttpGet]
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

            // Populating Dropdowns again for the edit view
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

    // POST: FitnessClasses/Edit/5
    [HttpPost]
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
                fitnessClass.ScheduleDate = fitnessClassViewModel.ScheduleDate;
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

    // POST: FitnessClasses/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Bookings) // Critical to include related bookings to check for existing reservations
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fitnessClass is null)
            {
                TempData["Error"] = "The class you are trying to delete was not found.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent deletion if there are existing bookings to avoid orphaned records and maintain data integrity
            if (fitnessClass.Bookings.Any())
            {
                TempData["Error"] = $"Action Denied: You Cannot delete {fitnessClass.Title} because it has {fitnessClass.Bookings.Count} " +
                                    $"Active reservations. Please cancel the booking first.";
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

    // GET: FitnessClasses/Attendance/5
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Attendance(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        try
        {
            // Deep Include: Class -> Bookings -> ApplicationUser
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
}