using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class InstructorViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Instructor name is required")]
    [StringLength(80)]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s'-]+$", ErrorMessage = "Only letters are allowed")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specialty is required")]
    [StringLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Required(ErrorMessage = "Instructor  email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Instructor phono is requered")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? Phone { get; set; }

    // NEW FIELD: Receives the uploaded photo file from the form
    [Display(Name = "Profile Photo")]
    public IFormFile? PhotoFile { get; set; }

    // Stores the existing photo name during Edit
    public string? ExistingPhoto { get; set; }
}