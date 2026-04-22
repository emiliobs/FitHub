using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitHub.Web.Controllers
{
    //Restrict access to Admins only for category management
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection of the DbContext
        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                // Retrieve all categories from the database
                var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                // Error dandling for database read operation
                TempData["Error"] = $"Could no load categories: {ex.Message}";
                return View(new List<Category>());
            }
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Category category)
        {
            // Server-side validation
            if (ModelState.IsValid)
            {
                try
                {
                    // Add the new category to the database
                    _context.Add(category);

                    // Save changes asynchronously
                    await _context.SaveChangesAsync();

                    // Success message for the UI
                    TempData["Success"] = "Category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Catch persistence error
                    TempData["Error"] = "Error to save changes. Try again.";
                }
            }

            // If we got this far, something failed, redisplay form with validation errors
            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Check if the id parameter is null
            if (id == null)
            {
                return NotFound();
            }

            // Find the category by id
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            // Pass the category to the view for editing
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            // Server-side validation
            if (ModelState.IsValid)
            {
                try
                {
                    // Update the category in the database
                    _context.Update(category);

                    // Save changes asynchronously
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Category Updating successfully!";

                    // Redirect to the index action after successful update
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Check if the category still exists in the database
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An eror ocurred while updating.";
                }
            }

            // If we got this far, something failed, redisplay form with validation errors
            return View(category);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Check if the id parameter is null
            if (id == null)
            {
                return NotFound();
            }

            // Find the category by id
            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Load the category along with related entities to check for dependencies
                var category = await _context.Categories
                    .Include(c => c.Instructors) // Assumming we have these relations
                    .Include(c => c.FitnessClasses) // Assumming we have these relations
                    .FirstOrDefaultAsync(m => m.Id == id);// Find the category by id

                // If the category is not found, return an error message
                if (category is null)
                {
                    TempData["Error"] = "The category you are trying to delete was not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check for related entities that would prevent deletion
                if (category.Instructors.Any())
                {
                    TempData["Error"] = $"Cannot delete {category.Name} because it is currently assigned to {category.Instructors.Count} " +
                                        $"instructor(s). Please reassign them first.";

                    return RedirectToAction(nameof(Index));
                }

                // Check for related fitness classes that would prevent deletion
                _context.Categories.Remove(category);

                // Save changes asynchronously
                await _context.SaveChangesAsync();

                // Success message for the UI
                TempData["Success"] = $"Category {category.Name} has been removed successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected server error occurred while trying to delete the category. " +
                                    "Please contact system support.";
            }

            // Redirect to the index action after attempting deletion
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if a category exists in the database
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}