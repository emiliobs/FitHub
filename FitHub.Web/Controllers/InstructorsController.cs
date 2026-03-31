using AspNetCoreGeneratedDocument;
using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

//Access restricted to Admin and Manager roles for security compliance

[Authorize(Roles = "Admin,Manager")]
public class InstructorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public InstructorsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        this._context = context;
        this._webHostEnvironment = webHostEnvironment;
    }

    //GET: Instructors/Index
    //Fetches all instructors and performs Eager Loading on the Category relationship

    public async Task<IActionResult> Index()
    {
        try
        {
            // Includes related Category data to display specialty names in the view
            var instructors = await _context.Instructors
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToListAsync();

            return View(instructors);
        }
        catch (Exception ex)
        {
            // Error handling with TempData feedback for the UI
            TempData["Error"] = $"An error occurred while loading instructors: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: Instructors/Details/{id}
    //Retrieves a single instructor profile including its relational category

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        // Database query with Join to Category table
        var instructor = await _context.Instructors
            .Include(i => i.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (instructor == null) return NotFound();

        return View(instructor);
    }

    // GET: Instructors/Create
    // Prepares the empty form and loads the category list
    public async Task<IActionResult> Create()
    {
        // Fetching categories from DB to populate the dropdown
        var categories = await _context.Categories.ToListAsync();

        var viewModel = new InstructorViewModel
        {
            // Projecting Category entities into SelectListItems for the UI dropdown
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
        };
        return View(viewModel);
    }

    // POST: Instructors/Create
    // Handles the submission and database insertion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InstructorViewModel instructorViewModel)
    {
        //We remove 'Category' because the form only sends the ID, not the full object.
        ModelState.Remove("Category");

        try
        {
            // Business Logic: Ensuring no duplicate emails exist in the database
            var emailExists = await _context.Instructors.AnyAsync(i => i.Email == instructorViewModel.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "An instructor with this email already exists.");
                TempData["Error"] = $"Error: An instructor with this {instructorViewModel.Email} already exists.";
            }

            // Checking if the refined model state is now valid
            if (ModelState.IsValid)
            {
                var fileName = "default-user.png";

                // Handling the physical file upload to the server
                if (instructorViewModel.PhotoFile != null)
                {
                    fileName = await UploadFile(instructorViewModel.PhotoFile);
                }

                // Creating the Domain Entity and mapping the CategoryId relationship
                var instructor = new Instructor
                {
                    Name = instructorViewModel.Name,
                    CategoryId = instructorViewModel.CategoryId, // Binding the Foreign Key
                    Email = instructorViewModel.Email,
                    Phone = instructorViewModel.Phone,
                    Photo = fileName
                };

                // Saving the new record to the database
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Instructor {instructor.Name} created successfully!";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            // Capturing database or IO errors to inform the user
            TempData["Error"] = $"An error occurred while creating the instructor: {ex.Message}";
        }

        /**
         * RECOVERY LOGIC: If the flow reaches here, it means validation failed.
         * We MUST reload the categories list and the view will crash during the re-rendering.
         */
        var categories = await _context.Categories.ToListAsync();
        instructorViewModel.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        });

        return View(instructorViewModel);
    }

    //GET: Instructors/Edit/{id}
    //Prepares the edit form with existing instructor data

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var instructor = await _context.Instructors.FindAsync(id);
        if (instructor == null) return NotFound();

        var categories = await _context.Categories.ToListAsync();

        var instructorViewModel = new InstructorViewModel
        {
            Id = instructor.Id,
            Name = instructor.Name,
            CategoryId = instructor.CategoryId,
            Email = instructor.Email,
            Phone = instructor.Phone,
            ExistingPhoto = instructor.Photo,
            // Mapping categories for the dropdown with the current value pre-selected
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
            })
        };

        return View(instructorViewModel);
    }

    //POST: Instructors/Edit
    //Updates existing instructor records and manages old file deletion

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InstructorViewModel instructorViewModel)
    {
        // FIX: Ignoring the navigation property during Model Binding validation
        ModelState.Remove("Category");

        try
        {
            if (ModelState.IsValid)
            {
                var instructor = await _context.Instructors.FindAsync(instructorViewModel.Id);
                if (instructor == null) return NotFound();

                // Updating textual and relational fields
                instructor.Name = instructorViewModel.Name;
                instructor.CategoryId = instructorViewModel.CategoryId;
                instructor.Email = instructorViewModel.Email;
                instructor.Phone = instructorViewModel.Phone;

                // Photo Update Logic: Replace old file with new one
                if (instructorViewModel.PhotoFile != null)
                {
                    // Deleting old file to save server storage space
                    if (instructor.Photo != "default-user.png")
                    {
                        DeleteFile(instructor.Photo);
                    }
                    instructor.Photo = await UploadFile(instructorViewModel.PhotoFile);
                }

                _context.Update(instructor);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Instructor {instructor.Name} updated successfully!";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred during update: {ex.Message}";
        }

        // Ensuring the dropdown remains populated if the view is returned with errors
        var categories = await _context.Categories.ToListAsync();
        instructorViewModel.Categories = categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });

        return View(instructorViewModel);
    }

    //POST: Instructors/DeleteFetch/{id}
    //Asynchronous endpoint for JS Fetch API deletion with physical file removal

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFetch(int id)
    {
        try
        {
            var instructor = await _context.Instructors.FindAsync(id);

            if (instructor == null)
            {
                return Json(new { success = false, message = "Record not found in database." });
            }

            // Cleanup: Removing profile photo from server disk
            if (!string.IsNullOrEmpty(instructor.Photo) && instructor.Photo != "default-user.png")
            {
                DeleteFile(instructor.Photo);
            }

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Instructor {instructor.Name} purged successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Deletion failed: " + ex.Message });
        }
    }

    //HELPER: UploadFile
    // Saves IFormFile to the server and returns the unique filename

    private async Task<string> UploadFile(IFormFile photoFile)
    {
        // Generating a unique ID to prevent filename collisions
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await photoFile.CopyToAsync(stream);
        }
        return fileName;
    }

    /**
     * HELPER: DeleteFile
     * Removes a specific file from the server's local storage
     */

    private void DeleteFile(string fileName)
    {
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}