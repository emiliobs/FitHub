using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

/// <summary>
/// Controller for class-driven subscription history.
/// </summary>
[Authorize]
public class SubscriptionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<SubscriptionsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Show latest active class subscription for the current user.
    /// </summary>
    public async Task<IActionResult> MySubscription()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var subscription = await _context.Subscriptions
            .Include(s => s.FitnessClass)
            .Include(s => s.Category)
            .Where(s => s.ApplicationUserId == user.Id && s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        return View(subscription);
    }

    /// <summary>
    /// Show subscription history
    /// </summary>
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var subscriptions = await _context.Subscriptions
            .Include(s => s.FitnessClass)
            .Include(s => s.Category)
            .Where(s => s.ApplicationUserId == user.Id)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        return View(subscriptions);
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int subscriptionId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
            s.Id == subscriptionId && s.ApplicationUserId == user.Id);

        if (subscription == null)
            return NotFound();

        try
        {
            subscription.IsActive = false;
            subscription.CancelledDate = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Subscription cancelled successfully.";
            return RedirectToAction(nameof(MySubscription));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            TempData["Error"] = "An error occurred while cancelling the subscription.";
            return RedirectToAction(nameof(MySubscription));
        }
    }
}


