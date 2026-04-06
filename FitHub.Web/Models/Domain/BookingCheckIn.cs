using FitHub.Web.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.Models.Domain;

public class BookingCheckIn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required]
    public int FitnessClassId { get; set; }

    [Required]
    [StringLength(32)]
    public string EnrollmentReference { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsRedeemed { get; set; }

    public DateTime? RedeemedAtUtc { get; set; }

    public virtual Booking Booking { get; set; } = null!;
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
    public virtual FitnessClass FitnessClass { get; set; } = null!;
}

