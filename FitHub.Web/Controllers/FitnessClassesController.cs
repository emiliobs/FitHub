using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers
{
    public class FitnessClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FitnessClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FitnessClasses - Public for browsing
        public async Task<IActionResult> Index()
        {
            var fitnessClasses = await _context.FitnessClasses
                .Include(f => f.Instructor)
                .Include(f => f.Category)
                .OrderBy(f => f.ScheduleDate)
                .ToListAsync();
            return View(fitnessClasses);
        }

        // GET: FitnessClasses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Instructor)
                .Include(f => f.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fitnessClass == null)
            {
                return NotFound();
            }

            return View(fitnessClass);
        }

        // GET: FitnessClasses/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name");
            return View();
        }

        // POST: FitnessClasses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Capacity,ScheduleDate,Price,InstructorId,CategoryId")] FitnessClass fitnessClass)
        {
            if (!ModelState.IsValid)
            {
                var errorMessages = new List<string>();
                foreach (var state in ModelState.Values)
                {
                    foreach (var error in state.Errors)
                    {
                        errorMessages.Add(error.ErrorMessage);
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }

                if (errorMessages.Count > 0)
                {
                    TempData["Error"] = "Validation errors: " + string.Join(", ", errorMessages);
                }

                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            if (fitnessClass.ScheduleDate <= DateTime.UtcNow)
            {
                ModelState.AddModelError("ScheduleDate", "Schedule date must be in the future.");
                TempData["Error"] = "Schedule date must be in the future.";
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            var instructor = await _context.Instructors.FindAsync(fitnessClass.InstructorId);
            if (instructor == null)
            {
                ModelState.AddModelError("InstructorId", "Selected instructor does not exist.");
                TempData["Error"] = "Selected instructor does not exist.";
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            var category = await _context.Categories.FindAsync(fitnessClass.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                TempData["Error"] = "Selected category does not exist.";
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            try
            {
                _context.Add(fitnessClass);
                int result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    TempData["Success"] = "Fitness class created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to save the class. Please try again.";
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                    ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                    return View(fitnessClass);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = $"Error creating class: {ex.Message}";
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }
        }

        // GET: FitnessClasses/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessClass = await _context.FitnessClasses.FindAsync(id);
            if (fitnessClass == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
            return View(fitnessClass);
        }

        // POST: FitnessClasses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Capacity,ScheduleDate,Price,InstructorId,CategoryId")] FitnessClass fitnessClass)
        {
            if (id != fitnessClass.Id)
            {
                TempData["Error"] = "ID mismatch. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            // Validate instructor exists
            var instructor = await _context.Instructors.FindAsync(fitnessClass.InstructorId);
            if (instructor == null)
            {
                ModelState.AddModelError("InstructorId", "Selected instructor does not exist.");
                TempData["Error"] = "Selected instructor does not exist.";
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            // Validate category exists
            var category = await _context.Categories.FindAsync(fitnessClass.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                TempData["Error"] = "Selected category does not exist.";
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            if (!ModelState.IsValid)
            {
                // Collect all validation errors
                var errorMessages = new List<string>();
                foreach (var state in ModelState.Values)
                {
                    foreach (var error in state.Errors)
                    {
                        errorMessages.Add(error.ErrorMessage);
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }

                // Show errors to user
                if (errorMessages.Count > 0)
                {
                    TempData["Error"] = "Validation errors: " + string.Join(", ", errorMessages);
                }

                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            try
            {
                // Verify the class exists in database first
                var existingClass = await _context.FitnessClasses.FindAsync(id);
                if (existingClass == null)
                {
                    TempData["Error"] = "Class not found in database.";
                    return RedirectToAction(nameof(Index));
                }

                // Update the properties
                existingClass.Title = fitnessClass.Title;
                existingClass.Description = fitnessClass.Description;
                existingClass.Capacity = fitnessClass.Capacity;
                existingClass.ScheduleDate = fitnessClass.ScheduleDate;
                existingClass.Price = fitnessClass.Price;
                existingClass.InstructorId = fitnessClass.InstructorId;
                existingClass.CategoryId = fitnessClass.CategoryId;

                // Save changes
                _context.Update(existingClass);
                int result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    TempData["Success"] = "Fitness class updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "No changes were saved. Please try again.";
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                    ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                    return View(fitnessClass);
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!FitnessClassExists(fitnessClass.Id))
                {
                    TempData["Error"] = "Class was deleted by another user.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = $"Concurrency error: {ex.Message}";
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                    ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                    return View(fitnessClass);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = $"Error updating class: {ex.Message}";
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", fitnessClass.CategoryId);
                ViewBag.InstructorId = new SelectList(_context.Instructors, "Id", "Name", fitnessClass.InstructorId);
                return View(fitnessClass);
            }
        }

        // GET: FitnessClasses/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Instructor)
                .Include(f => f.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fitnessClass == null)
            {
                return NotFound();
            }

            return View(fitnessClass);
        }

        // POST: FitnessClasses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fitnessClass = await _context.FitnessClasses.FindAsync(id);
            if (fitnessClass != null)
            {
                _context.FitnessClasses.Remove(fitnessClass);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fitness class deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool FitnessClassExists(int id)
        {
            return _context.FitnessClasses.Any(e => e.Id == id);
        }
    }
}

