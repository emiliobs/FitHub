using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

public class FitnessClass
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 200)]
    public int Capacity { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime ScheduleDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    // Foreign Key for Instructor (1:N)
    [Required]
    public int InstructorId { get; set; }

    public virtual Instructor Instructor { get; set; } = null!;

    // Foreign Key for Category (1:N)
    [Required]
    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    // Navigation property for the Many-to-Many relationship via Booking
    // Relación Muchos-a-Muchos con los usuarios a través de reservas
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}