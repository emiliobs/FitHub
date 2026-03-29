using FitHub.Web.Models.Identity;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        this._userManager = userManager;
        this._roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        var userList = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

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

        return View(userList);
    }

    // POST: /User/UpdateRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string newRole)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Safety check: Don't allow changing the role of the currently logged-in admin
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser?.Id == userId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove user from all current roles and add to the new role
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var resul = await _userManager.AddToRoleAsync(user, newRole);

            if (resul.Succeeded)
            {
                TempData["Success"] = $"Role updated to  -:{newRole}:- successfully for {user.FullName}.";
            }
            else
            {
                TempData["Error"] = $"Erro: Failed to update role.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"An error occurred: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}