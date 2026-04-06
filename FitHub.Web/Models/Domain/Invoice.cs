using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Domain;

/// <summary>
/// Represents an invoice for billing purposes
/// </summary>
public class Invoice
{
    [Key]
    public int Id { get; set; }

    // Foreign key to Payment
    [Required]
    public int PaymentId { get; set; }

    [ForeignKey("PaymentId")]
    public virtual Payment Payment { get; set; } = null!;

    [Required]
    [StringLength(50)]
    [Display(Name = "Invoice Number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Tax Amount")]
    public decimal TaxAmount { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Issue Date")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [StringLength(1000)]
    [Display(Name = "Invoice Description")]
    public string? Description { get; set; } // Description of what was purchased

    [StringLength(int.MaxValue)]
    [Display(Name = "PDF Content")]
    public string? PdfContent { get; set; } // Base64 encoded PDF or file path

    [Required]
    [Display(Name = "Is Paid")]
    public bool IsPaid { get; set; } = false;

    [DataType(DataType.DateTime)]
    [Display(Name = "Paid Date")]
    public DateTime? PaidDate { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}


