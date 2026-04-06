namespace FitHub.Web.Models.Domain;

/// <summary>
/// Enum for payment status
/// </summary>
public enum PaymentStatus
{
    Pending,      // Payment initiated but not processed
    Completed,    // Payment successfully processed
    Failed,       // Payment failed
    Cancelled,    // Payment cancelled by user
    Refunded      // Payment refunded
}

