using FitHub.Web.Models.Identity;
using FitHub.Web.Services;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    // Inject UserManager, RoleManager, and IInstructorSyncService through the constructor
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IInstructorSyncService _instructorSyncService;

    // Constructor to inject dependencies
    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IInstructorSyncService instructorSyncService)
    {
        this._userManager = userManager;
        this._roleManager = roleManager;
        this._instructorSyncService = instructorSyncService;
    }

    // GET: /User
    public async Task<IActionResult> Index()
    {
        //  Fetch all users and their roles
        var users = await _userManager.Users.ToListAsync();

        //  Fetch all roles to pass to the view
        var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        // Build a list of UserListViewModel to pass to the view
        var userList = new List<UserListViewModel>();

        //
        foreach (var user in users)
        {
            //
            var roles = await _userManager.GetRolesAsync(user);

            // For simplicity, we assume each user has only one role. If multiple roles are possible, this logic would need to be adjusted.
            userList.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Photo = user.Photo ?? "default-user.png",
                Role = roles.FirstOrDefault() ?? "No Role",
                AvailableRoles = allRoles! // Pass List to view
            });
        }

        // Pass the user list to the view
        return View(userList);
    }

    // POST: /User/UpdateRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string newRole)
    {
        try
        {
            // Validate the new role
            if (string.IsNullOrWhiteSpace(newRole) || !await _roleManager.RoleExistsAsync(newRole))
            {
                // Invalid role selected
                TempData["Error"] = "Invalid role selected.";

                //  Redirect back to the user list
                return RedirectToAction(nameof(Index));
            }

            // Find the user by ID
            var user = await _userManager.FindByIdAsync(userId);

            //  If user not found, return error
            if (user == null)
            {
                // User not found
                return NotFound("User not found.");
            }

            // Safety check: Don't allow changing the role of the currently logged-in admin
            var currentUser = await _userManager.GetUserAsync(User);

            // If the current user is trying to change their own role, prevent it
            if (currentUser?.Id == userId)
            {
                // Prevent admins from changing their own role to avoid locking themselves out of admin access
                TempData["Error"] = "You cannot change your own role.";

                // Redirect back to the user list
                return RedirectToAction(nameof(Index));
            }

            // Get current roles of the user
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove user from all current roles and add to the new role
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add user to the new role
            var resul = await _userManager.AddToRoleAsync(user, newRole);

            // If the new role is "Instructor", ensure the instructor profile is created or updated
            if (resul.Succeeded)
            {
                // If the new role is "Instructor", ensure the instructor profile is created or updated
                if (newRole == "Instructor")
                {
                    // Sync the instructor profile for the user. This will create a new profile if it doesn't exist or update the existing one.
                    var syncResult = await _instructorSyncService.EnsureInstructorForUserAsync(user);

                    // If the sync fails, we can choose to either revert the role change or just show an error message. Here, we'll just show an error message.
                    if (!syncResult.Succeeded)
                    {
                        // Log the error (not shown here) and inform the admin
                        TempData["Error"] = syncResult.ErrorMessage ?? "Role updated, but instructor profile sync failed.";

                        // Optionally, you could choose to revert the role change here if the instructor profile sync is critical. For now, we'll just inform the admin of the issue.
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Role update successful
                TempData["Success"] = $"Role updated to  -:{newRole}:- successfully for {user.FullName}.";
            }
            else
            {
                // Role update failed
                TempData["Error"] = $"Erro: Failed to update role.";
            }
        }
        catch (Exception ex)
        {
            // Log the exception (not shown here) and show a generic error message to the admin
            TempData["Error"] = $"An error occurred: {ex.Message}";
        }

        // Redirect back to the user list after processing
        return RedirectToAction(nameof(Index));
    }

    // POST: /User/DeleteUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            // Find the user by ID
            var user = await _userManager.FindByIdAsync(userId);

            // If user not found, return error
            if (user == null)
            {
                // User not found
                TempData["Error"] = "User not found.";

                //
                return RedirectToAction(nameof(Index));
            }

            // Safety Check 1: Prevent the currently logged-in admin from deleting themselves
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == userId)
            {
                // Prevent admins from deleting their own account to avoid accidental lockout
                TempData["Error"] = "Action Blocked: You cannot delete your own admin account.";

                //
                return RedirectToAction(nameof(Index));
            }

            // Safety Check 2: Prevent deletion of the ultimate Super Admin (Seed Admin)
            // This ensures the main owner account can never be removed from the system
            if (user.Email != null && user.Email.Equals("admin@yopmail.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Action Blocked: The primary Super Admin account cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            // Execute the deletion using ASP.NET Core Identity
            var result = await _userManager.DeleteAsync(user);

            // Check the result of the deletion operation
            if (result.Succeeded)
            {
                // Deletion successful
                TempData["Success"] = $"Warrior {user.FullName} has been permanently removed from the system.";
            }
            else
            {
                // Deletion failed - likely due to database constraints (e.g., related records that prevent deletion)
                TempData["Error"] = "Error: Failed to delete user. They may have dependent records in the database.";
            }
        }
        catch (Exception ex)
        {
            // Log the exception (not shown here) and show a generic error message to the admin
            TempData["Error"] = $"An error occurred during deletion: {ex.Message}";
        }

        // Redirect back to the user list after processing
        return RedirectToAction(nameof(Index));
    }
}