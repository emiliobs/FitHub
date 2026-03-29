using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "First Name is mandatory.")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is mandatory.")]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is mandatory.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is mandatory.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The password is too short.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is mandatory.")]
    [Display(Name = "Confirm password")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "The password is too short.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Profile Picture")]
    [DataType(DataType.Upload)]
    public IFormFile? PhotoFile { get; set; } // The physical file from the form
}