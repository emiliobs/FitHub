using FitHub.Web.Models.Domain;

namespace FitHub.Web.Services;

/// <summary>
/// Interface for invoice generation and management
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Generate an invoice for a payment
    /// </summary>
    Task<Invoice> GenerateInvoiceAsync(Payment payment, decimal taxRate = 0.1m);

    /// <summary>
    /// Get all invoices for a user
    /// </summary>
    Task<List<Invoice>> GetUserInvoicesAsync(string userId);

    /// <summary>
    /// Get an invoice by ID
    /// </summary>
    Task<Invoice?> GetInvoiceAsync(int invoiceId);

    /// <summary>
    /// Mark invoice as paid
    /// </summary>
    Task<bool> MarkInvoiceAsPaidAsync(int invoiceId);

    /// <summary>
    /// Generate invoice PDF content (mock)
    /// </summary>
    Task<string> GeneratePdfContentAsync(Invoice invoice);

    /// <summary>
    /// Get invoice number (formatted)
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync();
}

