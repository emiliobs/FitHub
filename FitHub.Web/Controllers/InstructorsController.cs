using AspNetCoreGeneratedDocument;
using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

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

    // GET: Instructors
    public async Task<IActionResult> Index()
    {
        try
        {
            var instructors = await _context.Instructors.OrderBy(i => i.Name).ToListAsync();

            return View(instructors);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred while loading instructors: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: Instructor/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // We fetch the instructor from the database
        var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.Id == id);

        if (instructor == null)
        {
            return NotFound();
        }

        return View(instructor);
    }

    // GET: INstructors/Create
    public IActionResult Create() => View();

    // POST: Instructors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InstructorViewModel instructorViewModel)
    {
        try
        {
            // Check if email already exists
            var emailExists = await _context.Instructors.AnyAsync(i => i.Email == instructorViewModel.Email);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "An instructor with this email already exists.");
            }

            if (ModelState.IsValid)
            {
                var fileName = "default-user.png";
                if (instructorViewModel.PhotoFile != null)
                {
                    fileName = await UploadFile(instructorViewModel.PhotoFile);
                }

                var instructor = new Instructor
                {
                    Name = instructorViewModel.Name,
                    Specialty = instructorViewModel.Specialty,
                    Email = instructorViewModel.Email,
                    Phone = instructorViewModel.Phone,
                    Photo = fileName
                };

                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Instructor {instructor.Name} created successfully!";

                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred while creating the instructor: {ex.Message}";
        }

        return View(instructorViewModel);
    }

    // GET: INstructors/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var instructor = await _context.Instructors.FindAsync(id);

        if (instructor == null)
        {
            return NotFound();
        }

        var instructorViewModel = new InstructorViewModel
        {
            Id = instructor!.Id,
            Name = instructor.Name,
            Specialty = instructor.Specialty,
            Email = instructor.Email,
            Phone = instructor.Phone,
            ExistingPhoto = instructor.Photo
        };

        return View(instructorViewModel);
    }

    // POST: Instructor?Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InstructorViewModel instructorViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var instructor = await _context.Instructors.FindAsync(instructorViewModel.Id);
                if (instructor == null)
                {
                    return NotFound();
                }

                instructor.Name = instructorViewModel.Name;
                instructor.Specialty = instructorViewModel.Specialty;
                instructor.Email = instructorViewModel.Email;
                instructor.Phone = instructorViewModel.Phone;

                if (instructorViewModel.PhotoFile != null)
                {
                    if (instructor.Phone != "default-user.png")
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
            TempData["Error"] = $"An error acurred while Editing the Instructor: {ex.Message}";
        }

        return View(instructorViewModel);
    }

    // POST: Instructors/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFetch(int id)
    {
        try
        {
            var instructor = await _context.Instructors.FindAsync(id);

            if (instructor == null)
            {
                return NotFound();
            }

            // Delete physical photo if it's no the default one
            if (!string.IsNullOrEmpty(instructor.Photo) && instructor.Phone != "default-user.png")
            {
                var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", instructor.Photo);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Instructor {instructor.Name} deleted successfully!" });
        }
        catch (Exception ex)
        {
            return Json(new { suceess = false, message = "Error during deletion: " + ex.Message });
        }
    }

    // POST

    // GET: Instructors/Delete/5
    //[HttpGet]
    //public async Task<IActionResult> Delete(int? id)
    //{
    //    if (id == null)
    //    {
    //        return NotFound();
    //    }

    //    var instructor = await _context.Instructors.FirstOrDefaultAsync(m => m.Id == id);

    //    if (instructor == null)
    //    {
    //        return NotFound();
    //    }

    //    return View(instructor);
    //}

    //// POST: Instructors/Delete/5
    //[HttpPost, ActionName("Delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteConfirm(int id)
    //{
    //    try
    //    {
    //        var instructor = await _context.Instructors.FindAsync(id);

    //        if (instructor != null)
    //        {
    //            // Delete physical photo if it's no the default one
    //            if (instructor.Phone != "default-user.png")
    //            {
    //                var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", instructor.Photo);

    //                if (System.IO.File.Exists(path))
    //                {
    //                    System.IO.File.Delete(path);
    //                }
    //            }

    //            // Remove the instructor from the database
    //            _context.Instructors.Remove(instructor);
    //            await _context.SaveChangesAsync();

    //            TempData["Success"] = $"Instructor {instructor.Name} deleted successfully!";
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        TempData["Error"] = $"Error: Cannot delete instructor: {ex.Message}";
    //    }

    //    return RedirectToAction("Index");
    //}

    // Helper method to handle file upload
    private async Task<string> UploadFile(IFormFile photoFile)
    {
        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await photoFile.CopyToAsync(stream);
        }

        return fileName;
    }

    private void DeleteFile(string fileName)
    {
        var path = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Profiles", fileName);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}