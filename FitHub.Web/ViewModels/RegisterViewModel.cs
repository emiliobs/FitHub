using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "First name is mandatory")]
    [Display(Name = "First Name")]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Numbers and special characters are not allowed in names")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is mandatory")]
    [Display(Name = "Last Name")]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Numbers and special characters are not allowed in names")]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Profile Picture")]
    public IFormFile? PhotoFile { get; set; }
}