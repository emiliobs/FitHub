using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

public class FitnessClass
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Class Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(1, 200, ErrorMessage = "Capacity must be between 1 and 200")]
    public int Capacity { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Class Schedule")]
    public DateTime ScheduleDate { get; set; }

    [Required]
    [Range(0, 500, ErrorMessage = "Price must be between 0 and 500")]
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

    // Bookings (Many-to-Many via Booking)
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [NotMapped]
    public int ActiveBookingsCount { get; set; }
}