using FitHub.Web.Data;
using FitHub.Web.Models.Identity;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager,
                             IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
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
            TempData["Error"] = $"Critical Error during login: {ex.Message}";
        }
        return View(loginViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors
                var errorMessages = new List<string>();
                foreach (var state in ModelState.Values)
                {
                    foreach (var error in state.Errors)
                    {
                        errorMessages.Add(error.ErrorMessage);
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }

                if (errorMessages.Count > 0)
                {
                    TempData["Error"] = "Validation errors: " + string.Join(", ", errorMessages);
                }

                return View(registerViewModel);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(registerViewModel.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered. Please use a different email or try logging in.");
                TempData["Error"] = "This email address is already registered. Please use a different email or login with your existing account.";
                return View(registerViewModel);
            }

            // Validate email format
            if (!registerViewModel.Email.Contains("@") || !registerViewModel.Email.Contains("."))
            {
                ModelState.AddModelError("Email", "Please enter a valid email address.");
                TempData["Error"] = "Please enter a valid email address.";
                return View(registerViewModel);
            }

            // Validate names are not empty
            if (string.IsNullOrWhiteSpace(registerViewModel.FirstName))
            {
                ModelState.AddModelError("FirstName", "First name is required.");
                TempData["Error"] = "First name is required.";
                return View(registerViewModel);
            }

            if (string.IsNullOrWhiteSpace(registerViewModel.LastName))
            {
                ModelState.AddModelError("LastName", "Last name is required.");
                TempData["Error"] = "Last name is required.";
                return View(registerViewModel);
            }

            // Note: We use the dynamic helper that accepts IFormFile
            var uniqueFileNamePhoto = ProcessUploadedFile(registerViewModel.PhotoFile);

            var user = new ApplicationUser
            {
                UserName = registerViewModel.Email,
                Email = registerViewModel.Email,
                FirstName = registerViewModel.FirstName,
                LastName = registerViewModel.LastName,
                Photo = uniqueFileNamePhoto,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerViewModel.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = "Welcome to the FitHub Energy family!";
                return RedirectToAction("Index", "Home");
            }

            // If creation failed, collect error messages
            if (!result.Succeeded)
            {
                var errorList = new List<string>();
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    errorList.Add(error.Description);
                    Console.WriteLine($"Registration Error: {error.Description}");
                }

                if (errorList.Count > 0)
                {
                    TempData["Error"] = "Registration failed: " + string.Join(", ", errorList);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Register Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            TempData["Error"] = $"Error registering: {ex.Message}";
        }

        return View(registerViewModel);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var model = new ProfileViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExistingPhoto = user.Photo
        };
        return View(model);
    }

    // [FIXED] Changed from [HttpGet] to [HttpPost]
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyProfile(ProfileViewModel profileViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(profileViewModel.Id);
                if (user == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id != user.Id) return Forbid();

                user.FirstName = profileViewModel.FirstName;
                user.LastName = profileViewModel.LastName;

                // Handle Photo Update
                if (profileViewModel.PhotoFile != null)
                {
                    if (!string.IsNullOrEmpty(user.Photo) && user.Photo != "default-user.png")
                    {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Profiles", user.Photo);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    user.Photo = ProcessUploadedFile(profileViewModel.PhotoFile);
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Your profile has been updated successfully!";
                    return RedirectToAction(nameof(MyProfile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error Updating Profile: {ex.Message}";
        }
        return View(profileViewModel);
    }

    // [IMPROVED] Helper now accepts IFormFile directly to be reusable
    private string ProcessUploadedFile(IFormFile? photoFile)
    {
        var uniqueFileNamePhoto = "default-user.png";

        if (photoFile != null)
        {
            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Profiles");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            uniqueFileNamePhoto = Guid.NewGuid().ToString() + "_" + photoFile.FileName;
            var filePath = Path.Combine(uploadFolder, uniqueFileNamePhoto);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                photoFile.CopyTo(fileStream);
            }
        }
        return uniqueFileNamePhoto;
    }

    // GET: /Account/AccessDenied
    public IActionResult AccessDenied()
    {
        return View();
    }
}