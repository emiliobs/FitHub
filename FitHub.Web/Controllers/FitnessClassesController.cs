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

        // Helper to avoid repetition
        private void PopulateDropdowns(int? selectedCategoryId = null, int? selectedInstructorId = null)
        {
            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", selectedCategoryId);
            ViewBag.InstructorList = new SelectList(_context.Instructors, "Id", "Name", selectedInstructorId);
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
                return NotFound();

            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Instructor)
                .Include(f => f.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fitnessClass == null)
                return NotFound();

            return View(fitnessClass);
        }

        // GET: FitnessClasses/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            PopulateDropdowns();
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
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Validation errors: " + string.Join(", ", errors);
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            if (fitnessClass.ScheduleDate <= DateTime.UtcNow)
            {
                ModelState.AddModelError("ScheduleDate", "Schedule date must be in the future.");
                TempData["Error"] = "Schedule date must be in the future.";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            var instructor = await _context.Instructors.FindAsync(fitnessClass.InstructorId);
            if (instructor == null)
            {
                ModelState.AddModelError("InstructorId", "Selected instructor does not exist.");
                TempData["Error"] = "Selected instructor does not exist.";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            var category = await _context.Categories.FindAsync(fitnessClass.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                TempData["Error"] = "Selected category does not exist.";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
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
                    PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                    return View(fitnessClass);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Create Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = $"Error creating class: {ex.Message}";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }
        }

        // GET: FitnessClasses/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var fitnessClass = await _context.FitnessClasses.FindAsync(id);
            if (fitnessClass == null)
                return NotFound();

            PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
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

            var instructor = await _context.Instructors.FindAsync(fitnessClass.InstructorId);
            if (instructor == null)
            {
                ModelState.AddModelError("InstructorId", "Selected instructor does not exist.");
                TempData["Error"] = "Selected instructor does not exist.";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            var category = await _context.Categories.FindAsync(fitnessClass.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Selected category does not exist.");
                TempData["Error"] = "Selected category does not exist.";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Validation errors: " + string.Join(", ", errors);
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }

            try
            {
                var existingClass = await _context.FitnessClasses.FindAsync(id);
                if (existingClass == null)
                {
                    TempData["Error"] = "Class not found in database.";
                    return RedirectToAction(nameof(Index));
                }

                existingClass.Title = fitnessClass.Title;
                existingClass.Description = fitnessClass.Description;
                existingClass.Capacity = fitnessClass.Capacity;
                existingClass.ScheduleDate = fitnessClass.ScheduleDate;
                existingClass.Price = fitnessClass.Price;
                existingClass.InstructorId = fitnessClass.InstructorId;
                existingClass.CategoryId = fitnessClass.CategoryId;

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
                    PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
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
                    PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                    return View(fitnessClass);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = $"Error updating class: {ex.Message}";
                PopulateDropdowns(fitnessClass.CategoryId, fitnessClass.InstructorId);
                return View(fitnessClass);
            }
        }

        // GET: FitnessClasses/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var fitnessClass = await _context.FitnessClasses
                .Include(f => f.Instructor)
                .Include(f => f.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (fitnessClass == null)
                return NotFound();

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