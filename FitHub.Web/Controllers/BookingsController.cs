using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitHub.Web.Controllers;

public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        this._context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    // GET : Bookings/MySchedule
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MySchedule()
    {
        // Get ID of the currently logged in user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            // challenge the user to log in if they are not authenticated
            return Challenge();
        }

        try
        {
            // Eager Loading: Fetching Bookings + Class + Instructor in one go
            var myBookings = await _context.Bookings
                .Include(b => b.FitnessClass)
                .ThenInclude(c => c.Instructor)
                .Include(b => b.FitnessClass.Category)
                .Where(b => b.ApplicationUserId == userId && b.Status == BookingStatus.Active)
                .OrderBy(b => b.FitnessClass.ScheduleDate)
                .ToListAsync();

            return View(myBookings);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: We couldn't load your training schedule. PLease try againg later: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> Cancel()
    {
        return RedirectToAction("MySchedule");
    }
}