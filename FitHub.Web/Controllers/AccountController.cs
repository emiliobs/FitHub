using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using FitHub.Web.Services;
using FitHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Web.Controllers;

public class AccountController : Controller
{
    // Added IInstructorSyncService for better separation of concerns and to handle instructor profile synchronization
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IInstructorSyncService _instructorSyncService;

    // Constructor injection of dependencies, including the new IInstructorSyncService
    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager,
                             IWebHostEnvironment webHostEnvironment,
                             IInstructorSyncService instructorSyncService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _webHostEnvironment = webHostEnvironment;
        _instructorSyncService = instructorSyncService;
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
        // Added try-catch for better error handling and user feedback
        try
        {
            // Check if the model state is valid before attempting to sign in
            if (ModelState.IsValid)
            {
                //  Attempt to sign in the user with the provided credentials
                var result = await _signInManager.PasswordSignInAsync(
                    loginViewModel.Email,
                    loginViewModel.Password,
                    loginViewModel.RememberMe,
                    lockoutOnFailure: false);

                // If the sign-in was successful, redirect to the return URL or home page
                if (result.Succeeded)
                {
                    // Optionally, you could add logging here for successful logins
                    TempData["Success"] = "Welcome back to FitHub Energy!";

                    // Ensure the return URL is local to prevent open redirect vulnerabilities
                    return LocalRedirect(returnUrl ?? "/");
                }

                // If the sign-in failed, add a model error and set a TempData message for feedback
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");

                //  Optionally, you could add logging here for failed login attempts
                TempData["Error"] = "Access denied. Check your credentials.";
            }
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here for brevity)
            TempData["Error"] = $"Critical Error during login: {ex.Message}";
        }
        //
        return View(loginViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Added try-catch for better error handling and user feedback during logout
        await _signInManager.SignOutAsync();

        // Optionally, you could add logging here for logout events
        TempData["Success"] = "You have been logged out.";

        // Redirect to the home page after logout
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Register
    public IActionResult Register()
    {
        // If the user is already authenticated, redirect them to the fitness classes page instead of showing the registration form
        if (User.Identity?.IsAuthenticated == true)
        {
            // Optionally, you could add logging here for authenticated users trying to access the registration page
            return RedirectToAction("Index", "FitnessClasses");
        }

        // Return the registration view with an empty model
        return View(new RegisterViewModel());
    }

    // Changed from [HttpGet] to [HttpPost] and added model validation and error handling
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
    {
        try
        {
            // Check if the model state is valid before attempting to create a new user
            if (ModelState.IsValid)
            {
                // Note: We use the dynamic helper that accepts IFormFile
                var uniqueFileNamePhoto = await ProcessUploadedFileAsync(registerViewModel.PhotoFile);

                // Create a new ApplicationUser instance with the provided registration details
                var user = new ApplicationUser
                {
                    UserName = registerViewModel.Email,
                    Email = registerViewModel.Email,
                    FirstName = registerViewModel.FirstName,
                    LastName = registerViewModel.LastName,
                    Photo = uniqueFileNamePhoto,
                    RegistrationDate = DateTime.UtcNow,
                    MembershipPlan = SubscriptionType.None,
                    SubscriptionEndDate = null
                };

                // Attempt to create the user with the specified password
                var result = await _userManager.CreateAsync(user, registerViewModel.Password);

                // If the user creation was successful, add them to the "Member" role and sign them in
                if (result.Succeeded)
                {
                    // Optionally, you could add logging here for successful registrations
                    await _userManager.AddToRoleAsync(user, "Member");

                    // Sign in the user immediately after successful registration
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Set a success message and redirect to the home page
                    TempData["Success"] = "Welcome to the FitHub Energy family!";

                    // Redirect to the home page after successful registration and login
                    return RedirectToAction("Index", "Home");
                }

                // If there were errors during user creation, add them to the model state and set a TempData message for feedback
                foreach (var error in result.Errors)
                {
                    // Add each error to the model state so they can be displayed in the view
                    ModelState.AddModelError(string.Empty, error.Description);
                    TempData["Error"] = "Registration failed: " + error.Description;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here for brevity)
            TempData["Error"] = $"Error register member: {ex.Message}";
        }

        return View(registerViewModel);
    }

    // GET: /Account/MyProfile
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyProfile()
    {
        // Retrieve the currently logged-in user
        var user = await _userManager.GetUserAsync(User);

        // If the user is not found (which should be rare since they are authenticated), return a 404 Not Found response
        if (user is null) return NotFound();

        // Map the user's details to the ProfileViewModel, including membership information for display in the profile view
        var model = new ProfileViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExistingPhoto = user.Photo,
            MembershipPlan = user.MembershipPlan,
            SubscriptionEndDate = user.SubscriptionEndDate,
        };

        // Return the profile view with the populated model
        return View(model);
    }

    // POST: /Account/MyProfile
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyProfile(ProfileViewModel profileViewModel)
    {
        // Added try-catch for better error handling and user feedback during profile updates
        try
        {
            // Check if the model state is valid before attempting to update the user's profile
            if (ModelState.IsValid)
            {
                // Retrieve the user from the database using the ID from the view model
                var user = await _userManager.FindByIdAsync(profileViewModel.Id);

                // If the user is not found, return a 404 Not Found response
                if (user == null)
                {
                    // Optionally, you could add logging here for cases where the user is not found during profile update attempts
                    return NotFound();
                }

                // Ensure that the currently logged-in user is the same as the user being updated to prevent unauthorized access
                var currentUser = await _userManager.GetUserAsync(User);

                // If the current user is not the same as the user being updated, return a 403 Forbidden response
                if (currentUser?.Id != user.Id)
                {
                    // Optionally, you could add logging here for unauthorized profile update attempts
                    return Forbid();
                }

                // UPDATE ONLY PERMITTED FIELDS
                user.FirstName = profileViewModel.FirstName;
                user.LastName = profileViewModel.LastName;

                // Handle Photo Update
                if (profileViewModel.PhotoFile != null)
                {
                    // If the user already has a custom photo (not the default), delete the old photo file to prevent orphaned
                    // files and save storage space
                    if (!string.IsNullOrEmpty(user.Photo) && user.Photo != "default-user.png")
                    {
                        // Construct the full path to the old photo file
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "Images", "Profiles", user.Photo);

                        // Check if the file exists before attempting to delete it to avoid exceptions
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    // Process the new uploaded photo file and update the user's Photo property with the new file name
                    user.Photo = await ProcessUploadedFileAsync(profileViewModel.PhotoFile);
                }

                // Attempt to update the user's profile in the database
                var result = await _userManager.UpdateAsync(user);

                // If the update was successful, check if the user is an instructor and ensure their instructor profile is synchronized
                if (result.Succeeded)
                {
                    //  If the user has the "Instructor" role, call the instructor sync service to ensure their instructor profile is
                    //  up to date
                    if (await _userManager.IsInRoleAsync(user, "Instructor"))
                    {
                        // Call the instructor sync service to ensure the instructor profile is created or updated as needed
                        var syncResult = await _instructorSyncService.EnsureInstructorForUserAsync(user);

                        // If the synchronization failed, set an error message but still allow the profile update to succeed since the main profile was
                        // updated successfully
                        if (!syncResult.Succeeded)
                        {
                            // Optionally, you could add logging here for instructor profile synchronization failures
                            TempData["Error"] = syncResult.ErrorMessage ?? "Profile updated, but instructor profile sync failed.";

                            // Redirect back to the profile page to show the error message without losing the updated profile information
                            return RedirectToAction(nameof(MyProfile));
                        }
                    }

                    // Optionally, you could add logging here for successful profile updates
                    TempData["Success"] = "Your profile has been updated successfully!";

                    // Redirect to the home page after successful profile update
                    return RedirectToAction("Index", "Home");
                }

                // If there were errors during the update, add them to the model state and set a TempData message for feedback
                foreach (var error in result.Errors)
                {
                    // Add each error to the model state so they can be displayed in the view
                    ModelState.AddModelError("", error.Description);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here for brevity)
            TempData["Error"] = $"Error Updating Profile: {ex.Message}";
        }

        // If validation fails, we must reload the membership data so the view doesn't break
        var userForReload = await _userManager.FindByIdAsync(profileViewModel.Id);

        // Reload the membership plan and subscription end date to ensure the view has the necessary data to display correctly,
        // even if the update failed
        if (userForReload != null)
        {
            // This ensures that the profile view can still display the user's membership information correctly, even if there
            // were validation errors during the update
            profileViewModel.MembershipPlan = userForReload.MembershipPlan;

            // This ensures that the profile view can still display the user's subscription end date correctly, even if there were
            profileViewModel.SubscriptionEndDate = userForReload.SubscriptionEndDate;
        }

        // Return the profile view with the original view model (which may contain validation errors) so the user can correct any issues
        return View(profileViewModel);
    }

    // This method processes the uploaded photo file, saves it to the server, and returns the unique file name to be stored
    // in the user's profile.
    private async Task<String> ProcessUploadedFileAsync(IFormFile? photoFile)
    {
        // Start with the default file name in case no file is uploaded or if there is an error during upload
        var uniqueFileNamePhoto = "default-user.png";

        // If a file was uploaded, attempt to save it to the server and generate a unique file name
        try
        {
            // Check if the uploaded file is not null before attempting to process it
            if (photoFile != null)
            {
                // Generate a unique file name using a GUID to prevent collisions and ensure each uploaded file has a unique name
                var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Profiles");

                // Ensure the upload directory exists; if it doesn't, create it to prevent errors when saving the file
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Combine the unique file name with the original file name to create a new unique file name for storage
                uniqueFileNamePhoto = Guid.NewGuid().ToString() + "_" + photoFile.FileName;

                // Construct the full file path where the uploaded file will be saved on the server
                var filePath = Path.Combine(uploadFolder, uniqueFileNamePhoto);

                // Save the uploaded file to the server using a file stream, and ensure that the operation is performed asynchronously to avoid blocking the thread
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    // Copy the contents of the uploaded file to the file stream, which saves it to the specified location on the server
                    await photoFile.CopyToAsync(fileStream);
                }
            }
        }
        catch (Exception)
        {
            // Log the error (not implemented here for brevity)
            uniqueFileNamePhoto = "default-user.png";
        }

        // Return the unique file name or default if upload failed
        return uniqueFileNamePhoto;
    }

    // GET: /Account/AccessDenied
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        // Simply returns the security error view;
        // the [Authorize] attribute will handle redirection to this page when access is denied
        return View();
    }
}