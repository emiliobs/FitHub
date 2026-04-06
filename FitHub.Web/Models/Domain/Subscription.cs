using FitHub.Web.Models.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

/// <summary>
/// Represents a user's purchased subscription for a specific fitness class/category.
/// </summary>
public class Subscription
{
    [Key]
    public int Id { get; set; }

    // Foreign key to ApplicationUser
    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [ForeignKey("ApplicationUserId")]
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;

    // The purchased class drives the subscription lifecycle.
    [Required]
    public int FitnessClassId { get; set; }

    [ForeignKey("FitnessClassId")]
    public virtual FitnessClass FitnessClass { get; set; } = null!;

    // Category is persisted for easier reporting/history snapshots.
    [Required]
    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Subscription Start Date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Subscription End Date")]
    public DateTime EndDate { get; set; }

    [Required]
    [Display(Name = "Auto Renew")]
    public bool AutoRenew { get; set; } = true;

    [Required]
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Price at Purchase")]
    public decimal PriceAtPurchase { get; set; } // Snapshot of price at time of purchase

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.DateTime)]
    [Display(Name = "Cancelled Date")]
    public DateTime? CancelledDate { get; set; }

    // Navigation property for payments
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Helper property to check if subscription is currently active
    /// </summary>
    [NotMapped]
    public bool IsCurrentlyActive => IsActive && EndDate >= DateTime.UtcNow && !CancelledDate.HasValue;

    /// <summary>
    /// Gets the days remaining in the subscription
    /// </summary>
    [NotMapped]
    public int DaysRemaining => Math.Max(0, (int)(EndDate - DateTime.UtcNow).TotalDays);

    /// <summary>
    /// Gets the renewal date (same as EndDate, but semantically different)
    /// </summary>
    [NotMapped]
    public DateTime RenewalDate => EndDate;
}


