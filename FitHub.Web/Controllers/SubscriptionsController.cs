using FitHub.Web.Data;
using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

// Controller for class-driven subscription history.
[Authorize]
public class SubscriptionsController : Controller
{
    // Dependency injection for database context, user manager, and logger.
    private readonly ApplicationDbContext _context;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SubscriptionsController> _logger;

    // Constructor to initialize dependencies.
    public SubscriptionsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<SubscriptionsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // Show latest active class subscription for the current user.

    public async Task<IActionResult> MySubscription()
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }

        // Get the latest active subscription for the user
        var subscription = await _context.Subscriptions
            .Include(s => s.FitnessClass) //    Include related fitness class details
            .Include(s => s.Category)//   Include related category details
            .Where(s => s.ApplicationUserId == user.Id && s.IsActive)//   Filter for active subscriptions of the current user
            .OrderByDescending(s => s.StartDate)//   Order by start date to get the latest subscription
            .FirstOrDefaultAsync();

        return View(subscription);
    }

    // Show subscription history
    public async Task<IActionResult> History()
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);

        // If user is not found, return unauthorized
        if (user == null)
        {
            return Unauthorized();
        }

        // Get all subscriptions for the user, including related fitness class and category details, ordered by start date
        var subscriptions = await _context.Subscriptions
            .Include(s => s.FitnessClass) //   Include related fitness class details
            .Include(s => s.Category) //   Include related category details
            .Where(s => s.ApplicationUserId == user.Id)//  Filter for subscriptions of the current user
            .OrderByDescending(s => s.StartDate)// Order by start date to show the most recent subscriptions first
            .ToListAsync();// Execute the query and get the list of subscriptions

        // Pass the subscriptions to the view
        return View(subscriptions);
    }

    //Cancel subscription
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int subscriptionId)
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        // Find the subscription by ID and ensure it belongs to the current user
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
            s.Id == subscriptionId && s.ApplicationUserId == user.Id);

        if (subscription == null)
        {
            return NotFound();
        }

        try
        {
            // Mark the subscription as cancelled
            subscription.IsActive = false;
            subscription.CancelledDate = DateTime.UtcNow;

            // Update the subscription in the database
            _context.Subscriptions.Update(subscription);

            //
            await _context.SaveChangesAsync();

            TempData["Success"] = "Subscription cancelled successfully.";
            return RedirectToAction(nameof(MySubscription));
        }
        catch (Exception ex)
        {
            // Log the error and show an error message to the user
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            TempData["Error"] = "An error occurred while cancelling the subscription.";

            // Redirect back to the subscription details page
            return RedirectToAction(nameof(MySubscription));
        }
    }
}