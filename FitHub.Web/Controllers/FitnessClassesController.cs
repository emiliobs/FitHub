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
}