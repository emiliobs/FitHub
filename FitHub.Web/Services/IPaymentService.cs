using FitHub.Web.Models.Domain;

namespace FitHub.Web.Services;

/// <summary>
/// Result of a payment operation
/// </summary>
public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Payment? Payment { get; set; }
}

public class StripeCheckoutSessionResult
{
    public bool IsSuccess { get; set; }
    public string? CheckoutUrl { get; set; }
    public string? SessionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StripePaymentVerificationResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Interface for payment processing service
/// </summary>
public interface IPaymentService
{
    Task<StripeCheckoutSessionResult> CreateStripeCheckoutSessionAsync(
        string userId,
        int classId,
        string classTitle,
        decimal amount,
        string successUrl,
        string cancelUrl);

    Task<StripePaymentVerificationResult> VerifyStripeCheckoutPaymentAsync(string sessionId, string userId, int classId);

    /// <summary>
    /// Process a payment for a subscription
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(Subscription subscription, string paymentMethod = "CreditCard", string? cardToken = null);

    /// <summary>
    /// Refund a payment
    /// </summary>
    Task<PaymentResult> RefundPaymentAsync(Payment payment, string? reason = null);

    /// <summary>
    /// Get payment history for a user
    /// </summary>
    Task<List<Payment>> GetPaymentHistoryAsync(string userId);

    /// <summary>
    /// Validate a mock payment token
    /// </summary>
    bool ValidateToken(string? token);
}

