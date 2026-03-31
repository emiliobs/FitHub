using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.Models.Domain;

public class Instructor
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Instructor name is mandatory")]
    [StringLength(80)]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s'-]+$", ErrorMessage = "Only letters are allowed")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Required(ErrorMessage = "Instructor  email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Instructor phono is requered")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? Phone { get; set; }

    // NEW FIELD: Stores the filename of the instructor's photo
    public string Photo { get; set; } = "default-user.png";

    //Foreign Key: Link to the Category ID
    public int CategoryId { get; set; }

    // Navigation property to access Category data
    public virtual Category? Category { get; set; }

    // Navigation property
    public virtual ICollection<FitnessClass> FitnessClasses { get; set; } = new List<FitnessClass>();
}