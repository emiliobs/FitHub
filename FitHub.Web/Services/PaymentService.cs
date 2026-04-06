using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StripeConfiguration = Stripe.StripeConfiguration;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionLineItemPriceDataOptions = Stripe.Checkout.SessionLineItemPriceDataOptions;
using CheckoutSessionLineItemPriceDataProductDataOptions = Stripe.Checkout.SessionLineItemPriceDataProductDataOptions;

namespace FitHub.Web.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly StripeOptions _stripeOptions;
    private readonly Random _random = new();

    public PaymentService(
        ApplicationDbContext context,
        ILogger<PaymentService> logger,
        IOptions<StripeOptions> stripeOptions)
    {
        _context = context;
        _logger = logger;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<StripeCheckoutSessionResult> CreateStripeCheckoutSessionAsync(
        string userId,
        int classId,
        string classTitle,
        decimal amount,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
            {
                return new StripeCheckoutSessionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Stripe secret key is not configured."
                };
            }

            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

            var currency = string.IsNullOrWhiteSpace(_stripeOptions.Currency)
                ? "gbp"
                : _stripeOptions.Currency.ToLowerInvariant();

            var amountInMinorUnits = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

            var options = new CheckoutSessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = userId,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    ["UserId"] = userId,
                    ["ClassId"] = classId.ToString()
                },
                LineItems = new List<CheckoutSessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new CheckoutSessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = amountInMinorUnits,
                            ProductData = new CheckoutSessionLineItemPriceDataProductDataOptions
                            {
                                Name = classTitle
                            }
                        }
                    }
                }
            };

            var sessionService = new CheckoutSessionService();
            var session = await sessionService.CreateAsync(options);

            return new StripeCheckoutSessionResult
            {
                IsSuccess = !string.IsNullOrWhiteSpace(session.Url),
                CheckoutUrl = session.Url,
                SessionId = session.Id,
                ErrorMessage = string.IsNullOrWhiteSpace(session.Url) ? "Could not create Stripe checkout session." : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for class {ClassId}", classId);
            return new StripeCheckoutSessionResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to initialize secure payment gateway."
            };
        }
    }

    public async Task<StripePaymentVerificationResult> VerifyStripeCheckoutPaymentAsync(string sessionId, string userId, int classId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
            {
                return new StripePaymentVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Stripe secret key is not configured."
                };
            }

            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;

            var sessionService = new CheckoutSessionService();
            var session = await sessionService.GetAsync(sessionId);

            if (session == null)
            {
                return new StripePaymentVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment session was not found."
                };
            }

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                return new StripePaymentVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment is not completed yet."
                };
            }

            var metadataUserId = session.Metadata != null && session.Metadata.TryGetValue("UserId", out var mUserId) ? mUserId : null;
            var metadataClassId = session.Metadata != null && session.Metadata.TryGetValue("ClassId", out var mClassId) ? mClassId : null;

            if (!string.Equals(metadataUserId, userId, StringComparison.Ordinal))
            {
                return new StripePaymentVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment session does not belong to current user."
                };
            }

            if (!int.TryParse(metadataClassId, out var parsedClassId) || parsedClassId != classId)
            {
                return new StripePaymentVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Payment session class metadata mismatch."
                };
            }

            return new StripePaymentVerificationResult
            {
                IsSuccess = true,
                TransactionId = string.IsNullOrWhiteSpace(session.PaymentIntentId) ? session.Id : session.PaymentIntentId,
                AmountPaid = (session.AmountTotal ?? 0) / 100m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Stripe checkout session {SessionId}", sessionId);
            return new StripePaymentVerificationResult
            {
                IsSuccess = false,
                ErrorMessage = "Unable to verify Stripe payment session."
            };
        }
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Subscription subscription, string paymentMethod = "CreditCard", string? cardToken = null)
    {
        try
        {
            if (!ValidateToken(cardToken))
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid payment token. Please use a valid credit card.",
                    TransactionId = GenerateMockTransactionId()
                };
            }

            var paymentSucceeds = _random.Next(0, 100) < 95;

            var payment = new Payment
            {
                SubscriptionId = subscription.Id,
                Amount = subscription.PriceAtPurchase,
                PaymentMethod = paymentMethod,
                TransactionId = GenerateMockTransactionId(),
                PaymentDate = DateTime.UtcNow,
                Status = paymentSucceeds ? PaymentStatus.Completed : PaymentStatus.Failed,
                Notes = paymentSucceeds ? "Mock payment processed successfully" : "Simulated payment failure - please try again"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} processed with status: {Status}", payment.Id, payment.Status);

            return new PaymentResult
            {
                IsSuccess = paymentSucceeds,
                TransactionId = payment.TransactionId,
                ErrorMessage = paymentSucceeds ? null : payment.Notes,
                Payment = payment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for subscription {SubscriptionId}", subscription.Id);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Payment processing error: {ex.Message}",
                TransactionId = GenerateMockTransactionId()
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(Payment payment, string? reason = null)
    {
        try
        {
            if (payment.Status != PaymentStatus.Completed)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot refund a payment with status '{payment.Status}'",
                    TransactionId = payment.TransactionId
                };
            }

            payment.Status = PaymentStatus.Refunded;
            payment.RefundDate = DateTime.UtcNow;
            payment.Notes = $"Refunded on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}. Reason: {reason ?? "No reason provided"}";

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} refunded. Reason: {Reason}", payment.Id, reason);

            return new PaymentResult
            {
                IsSuccess = true,
                TransactionId = payment.TransactionId,
                Payment = payment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", payment.Id);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Refund processing error: {ex.Message}",
                TransactionId = payment.TransactionId
            };
        }
    }

    public async Task<List<Payment>> GetPaymentHistoryAsync(string userId)
    {
        return await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .Include(p => p.Subscription)
            .ThenInclude(s => s.Category)
            .Where(p => p.Subscription.ApplicationUserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var normalized = token.Replace("-", "").Replace(" ", "");
        if (normalized.Length < 13 || !normalized.All(char.IsDigit))
        {
            return false;
        }

        if (_random.Next(0, 100) < 99)
        {
            return true;
        }

        _logger.LogWarning("Token validation failed for token prefix: {Prefix}", normalized[..4]);
        return false;
    }

    private static string GenerateMockTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(10000, 99999)}";
    }
}

