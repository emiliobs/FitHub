using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First Name is required")]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required")]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Current Profile Picture")]
    public string? ExistingPhoto { get; set; }

    [Display(Name = "Upload New Photo")]
    public IFormFile? PhotoFile { get; set; }
}