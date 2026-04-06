using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

/// <summary>
/// Represents a payment transaction for a subscription
/// </summary>
public class Payment
{
    [Key]
    public int Id { get; set; }

    // Foreign key to Subscription
    [Required]
    public int SubscriptionId { get; set; }

    [ForeignKey("SubscriptionId")]
    public virtual Subscription Subscription { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "CreditCard"; // CreditCard, PayPal, etc.

    [Required]
    [Display(Name = "Payment Status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [Required]
    [StringLength(100)]
    [Display(Name = "Transaction ID")]
    public string TransactionId { get; set; } = string.Empty; // Mock or real transaction ID

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.DateTime)]
    [Display(Name = "Refund Date")]
    public DateTime? RefundDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; } // For storing error messages or refund reasons

    // Navigation property for Invoice
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

