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
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        this._webHostEnvironment = webHostEnvironment;
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

    // GET: /Account/Register
    public IActionResult Register() => View();

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                // Process the photo before creating the user
                //var uniqueFileNamePhoto = ProcessUploadedFile(registerViewModel);

                var user = new ApplicationUser
                {
                    UserName = registerViewModel.Email,
                    Email = registerViewModel.Email,
                    FirstName = registerViewModel.FirstName,
                    LastName = registerViewModel.LastName,
                    //Photo = uniqueFileNamePhoto,
                    RegistrationDate = DateTime.UtcNow, // Auto-set registraction date
                };

                var result = await _userManager.CreateAsync(user, registerViewModel.Password);

                if (result.Succeeded)
                {
                    // Assign a default role (Member)
                    await _userManager.AddToRoleAsync(user, "Member");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["Success"] = "Welcome to the FitHub Energy family!";

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    TempData["Error"] = $"Error Register member: {error.Description}";
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error register member: {ex.Message}";
        }

        return View(registerViewModel);
    }

    //private string ProcessUploadedFile(RegisterViewModel registerViewModel)
    //{
    //    var uniqueFileNamePhoto = "default-user.png";

    //    if (registerViewModel. PhotoFile != null)
    //    {
    //        var uploadFolder
    //    };
    //}
}