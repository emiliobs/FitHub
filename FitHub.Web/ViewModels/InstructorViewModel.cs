using Microsoft.AspNetCore.Mvc.Rendering;
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

    
    // Este ID recibirá el valor seleccionado del menú desplegable
    [Required(ErrorMessage = "Please select a specialty")]
    [Display(Name = "Specialty Category")]
    public int CategoryId { get; set; }

    // List used ONLY to populate the HTML Select, not saved in DB
    public IEnumerable<SelectListItem>? Categories { get; set; }
}