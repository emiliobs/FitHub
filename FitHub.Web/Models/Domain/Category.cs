using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.Models.Domain;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation property for the One-to-Many relationship
    // Una categoría puede tener múltiples clases asociadas
    public virtual ICollection<FitnessClass> FitnessClasses { get; set; } = new List<FitnessClass>();
}