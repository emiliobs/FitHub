using FitHub.Web.Models.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

public class Booking
{
    [Key]
    public int Id { get; set; }

    // Link to the Identity User
    [Required]
    public string ApplicationUserId { get; set; } = null!;

    public virtual ApplicationUser ApplicationUser { get; set; } = null!;

    // Link to the Fitness Class
    [Required]
    public int FitnessClassId { get; set; }

    public virtual FitnessClass FitnessClass { get; set; } = null!;

    [Required]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    // Snapshot of the price at the moment of booking (Historical Data)
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidPrice { get; set; }

    // Optional notes for the instructor or the warrior
    [MaxLength(500)]
    public string? InternalNotes { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Active;
}