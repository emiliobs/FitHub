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

            // The view will iterate over the instructors and can access the Category.Name property directly
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
            .Include(i => i.Category)// Eager loading to fetch related category data in the same query
            .FirstOrDefaultAsync(m => m.Id == id);// Fetches the instructor with the specified ID along with its category

        if (instructor == null)
        {
            return NotFound();
        }

        // The view can now display instructor details along with the category name using instructor.Category.Name
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
                // Default photo assignment for instructors without an uploaded image
                var fileName = "default-user.png";

                // Handling the physical file upload to the server
                if (instructorViewModel.PhotoFile != null)
                {
                    // Uploading the file and getting the unique filename to store in the database
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

        // If we reach this point, something went wrong. We need to reload the categories for the dropdown.
        var categories = await _context.Categories.ToListAsync();

        // Re-populating the Categories for the dropdown in case of an error to ensure the form remains functional
        instructorViewModel.Categories = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name
        });

        // Returning the view with the original data and error messages for user correction
        return View(instructorViewModel);
    }

    //GET: Instructors/Edit/{id}
    //Prepares the edit form with existing instructor data

    public async Task<IActionResult> Edit(int? id)
    {
        // Validating the presence of the ID parameter to prevent null reference errors
        if (id == null)
        {
            return NotFound();
        }

        // Fetching the instructor record to be edited
        var instructor = await _context.Instructors.FindAsync(id);

        // If the record doesn't exist, return a 404 Not Found response
        if (instructor == null)
        {
            return NotFound();
        }

        // Fetching categories to populate the dropdown in the edit form
        var categories = await _context.Categories.ToListAsync();

        // Mapping the existing instructor data to the ViewModel for the edit form
        var instructorViewModel = new InstructorViewModel
        {
            Id = instructor.Id,
            Name = instructor.Name,
            CategoryId = instructor.CategoryId,
            Email = instructor.Email,
            Phone = instructor.Phone,
            ExistingPhoto = instructor.Photo,
            Categories = categories.Select(c => new SelectListItem // Projecting Category entities into SelectListItems for the UI dropdown
            {
                Value = c.Id.ToString(),
                Text = c.Name,
            })
        };

        // Returning the edit view with the populated ViewModel to allow the user to make changes
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
                // Fetching the existing instructor record from the database to update
                var instructor = await _context.Instructors.FindAsync(instructorViewModel.Id);

                if (instructor == null)
                {
                    return NotFound();
                }

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
                        // Only delete if it's not the default image to prevent accidental removal of shared assets
                        DeleteFile(instructor.Photo);
                    }

                    // Uploading the new file and updating the database record with the new filename
                    instructor.Photo = await UploadFile(instructorViewModel.PhotoFile);
                }

                // Saving the updated record to the database
                _context.Update(instructor);

                // Committing the transaction to persist changes
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
            // Fetching the instructor record to be deleted from the database
            var instructor = await _context.Instructors.FindAsync(id);

            // If the record doesn't exist, return a JSON response indicating failure
            if (instructor == null)
            {
                // This can happen if the record was already deleted or if an invalid ID was provided
                return Json(new { success = false, message = "Record not found in database." });
            }

            // Cleanup: Removing profile photo from server disk
            if (!string.IsNullOrEmpty(instructor.Photo) && instructor.Photo != "default-user.png")
            {
                // Only delete if it's not the default image to prevent accidental removal of shared assets
                DeleteFile(instructor.Photo);
            }

            // Removing the instructor record from the database
            _context.Instructors.Remove(instructor);

            // Committing the transaction to persist the deletion
            await _context.SaveChangesAsync();

            // Returning a JSON response indicating successful deletion to the client-side JavaScript
            return Json(new { success = true, message = $"Instructor {instructor.Name} purged successfully!" });
        }
        catch (Exception ex)
        {
            // Capturing any exceptions that occur during the deletion process and returning a JSON response with the error message
            return Json(new { success = false, message = "Deletion failed: " + ex.Message });
        }
    }

    //HELPER: UploadFile. Saves IFormFile to the server and returns the unique filename

    private async Task<string> UploadFile(IFormFile photoFile)
    {
        // Generating a unique ID to prevent filename collisions
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);

        // Constructing the full path to save the file in the wwwroot/Images/Profiles directory
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);

        // Saving the file to the server's local storage
        using (var stream = new FileStream(path, FileMode.Create))
        {
            // Asynchronously copying the uploaded file's content to the new file stream on the server
            await photoFile.CopyToAsync(stream);
        }

        // Returning the unique filename to be stored in the database for later retrieval
        return fileName;
    }

    //DeleteFile, Removes a specific file from the server's local storage
    private void DeleteFile(string fileName)
    {
        // Constructing the full path to the file to be deleted in the wwwroot/Images/Profiles directory
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);

        // Checking if the file exists before attempting deletion to prevent exceptions
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}