using FitHub.Web.Data;
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
            TempData["ErrorMessage"] = $"An error occurred while loading instructors: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }
}