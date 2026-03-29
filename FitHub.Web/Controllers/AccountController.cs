using FitHub.Web.Data;
using FitHub.Web.Models.Identity;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers;

public class AccountController : Controller
{
    // [CORRECTED] Use ApplicationUser, NOT the DbContext
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        // [CORRECTED] Use dot notation for ViewBag or brackets for ViewData
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel, string? returnUrl = null)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Identity handles the password hashing and verification automatically
                var result = await _signInManager.PasswordSignInAsync(
                    loginViewModel.Email,
                    loginViewModel.Password,
                    loginViewModel.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Welcome back to FitHub Energy!";
                    return LocalRedirect(returnUrl ?? "/");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                TempData["Error"] = "Access denied. Check your credentials.";
            }
        }
        catch (Exception ex)
        {
            // Log the error (crucial for your coursework report)
            TempData["Error"] = $"Critical Error during login: {ex.Message}";
        }

        return View(loginViewModel);
    }

    // PSOT: /Account/Logout

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()

    {
        await _signInManager.SignOutAsync();

        TempData["Success"] = "You have been logged out.";

        return RedirectToAction("Index", "Home");
    }
}