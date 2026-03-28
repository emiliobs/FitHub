using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.Models.Domain;

public class Instructor
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Instructor name is mandatory")]
    [StringLength(80)]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s'-]+$", ErrorMessage = "Invalid characters in name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    // Navigation property: An instructor can be assigned to many classes
    // Un instructor puede estar asignado a muchas clases
    public virtual ICollection<FitnessClass> FitnessClasses { get; set; } = new List<FitnessClass>();
}