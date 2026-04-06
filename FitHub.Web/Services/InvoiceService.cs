using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FitHub.Web.Services;

/// <summary>
/// Service for generating and managing invoices
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(ApplicationDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate an invoice for a payment
    /// </summary>
    public async Task<Invoice> GenerateInvoiceAsync(Payment payment, decimal taxRate = 0.1m)
    {
        try
        {
            var paymentWithDetails = await _context.Payments
                .Include(p => p.Subscription)
                .ThenInclude(s => s.FitnessClass)
                .Include(p => p.Subscription)
                .ThenInclude(s => s.Category)
                .Include(p => p.Subscription)
                .ThenInclude(s => s.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == payment.Id);

            if (paymentWithDetails == null)
            {
                throw new InvalidOperationException($"Payment {payment.Id} was not found.");
            }

            var invoiceNumber = await GenerateInvoiceNumberAsync();
            var subtotal = paymentWithDetails.Amount;
            var taxAmount = subtotal * taxRate;
            var totalAmount = subtotal + taxAmount;

            var invoice = new Invoice
            {
                PaymentId = paymentWithDetails.Id,
                InvoiceNumber = invoiceNumber,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // Due in 30 days
                Description = $"Class reservation for {paymentWithDetails.Subscription.FitnessClass.Title} ({paymentWithDetails.Subscription.Category.Name})",
                IsPaid = paymentWithDetails.Status == PaymentStatus.Completed,
                PaidDate = paymentWithDetails.Status == PaymentStatus.Completed ? DateTime.UtcNow : null,
                CreatedDate = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} generated for payment {PaymentId}", invoiceNumber, paymentWithDetails.Id);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for payment {PaymentId}", payment.Id);
            throw;
        }
    }

    /// <summary>
    /// Get all invoices for a user
    /// </summary>
    public async Task<List<Invoice>> GetUserInvoicesAsync(string userId)
    {
        return await _context.Invoices
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.ApplicationUser)
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.Category)
            .Where(i => i.Payment.Subscription.ApplicationUserId == userId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get an invoice by ID
    /// </summary>
    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        return await _context.Invoices
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.ApplicationUser)
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    /// <summary>
    /// Mark invoice as paid
    /// </summary>
    public async Task<bool> MarkInvoiceAsPaidAsync(int invoiceId)
    {
        try
        {
            var invoice = await GetInvoiceAsync(invoiceId);
            if (invoice == null)
            {
                return false;
            }

            invoice.IsPaid = true;
            invoice.PaidDate = DateTime.UtcNow;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} marked as paid", invoice.InvoiceNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", invoiceId);
            return false;
        }
    }

    /// <summary>
    /// Generate invoice PDF content as HTML/Text (mock implementation)
    /// In a real application, you would use a library like iTextSharp or SelectPdf
    /// </summary>
    public async Task<string> GeneratePdfContentAsync(Invoice invoice)
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<title>Invoice</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            sb.AppendLine(".invoice-details { display: flex; justify-content: space-between; margin-bottom: 30px; }");
            sb.AppendLine(".section { margin-bottom: 20px; }");
            sb.AppendLine(".label { font-weight: bold; }");
            sb.AppendLine(".amount { text-align: right; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 10px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine(".total-row { font-weight: bold; background-color: #f2f2f2; }");
            sb.AppendLine(".footer { margin-top: 30px; text-align: center; color: #666; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1>FitHub</h1>");
            sb.AppendLine("<h2>INVOICE</h2>");
            sb.AppendLine("</div>");

            // Invoice Info
            sb.AppendLine("<div class=\"invoice-details\">");
            sb.AppendLine($"<div class=\"section\">");
            sb.AppendLine($"<p><span class=\"label\">Invoice Number:</span> {invoice.InvoiceNumber}</p>");
            sb.AppendLine($"<p><span class=\"label\">Invoice Date:</span> {invoice.IssueDate:yyyy-MM-dd}</p>");
            sb.AppendLine($"<p><span class=\"label\">Due Date:</span> {invoice.DueDate:yyyy-MM-dd}</p>");
            sb.AppendLine($"</div>");
            sb.AppendLine($"<div class=\"section amount\">");
            sb.AppendLine($"<p><span class=\"label\">Status:</span> {(invoice.IsPaid ? "PAID" : "PENDING")}</p>");
            sb.AppendLine($"</div>");
            sb.AppendLine("</div>");

            // Bill To
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine("<p><span class=\"label\">Bill To:</span></p>");
            sb.AppendLine($"<p>{invoice.Payment.Subscription.ApplicationUser.FullName}</p>");
            sb.AppendLine($"<p>Email: {invoice.Payment.Subscription.ApplicationUser.Email}</p>");
            sb.AppendLine("</div>");

            // Item Details
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>Description</th>");
            sb.AppendLine("<th class=\"amount\">Quantity</th>");
            sb.AppendLine("<th class=\"amount\">Unit Price</th>");
            sb.AppendLine("<th class=\"amount\">Amount</th>");
            sb.AppendLine("</tr>");
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{invoice.Description}</td>");
            sb.AppendLine($"<td class=\"amount\">1</td>");
            sb.AppendLine($"<td class=\"amount\">${invoice.Subtotal:F2}</td>");
            sb.AppendLine($"<td class=\"amount\">${invoice.Subtotal:F2}</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("<tr class=\"total-row\"><td colspan=\"3\">Subtotal</td><td class=\"amount\">" + $"${invoice.Subtotal:F2}" + "</td></tr>");
            sb.AppendLine("<tr><td colspan=\"3\">Tax (10%)</td><td class=\"amount\">" + $"${invoice.TaxAmount:F2}" + "</td></tr>");
            sb.AppendLine("<tr class=\"total-row\"><td colspan=\"3\">TOTAL</td><td class=\"amount\">" + $"${invoice.TotalAmount:F2}" + "</td></tr>");
            sb.AppendLine("</table>");

            // Payment Info
            sb.AppendLine("<div class=\"section\">");
            sb.AppendLine($"<p><span class=\"label\">Payment Method:</span> {invoice.Payment.PaymentMethod}</p>");
            sb.AppendLine($"<p><span class=\"label\">Transaction ID:</span> {invoice.Payment.TransactionId}</p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class=\"footer\">");
            sb.AppendLine("<p>Thank you for your business!</p>");
            sb.AppendLine("<p>If you have any questions about this invoice, please contact us at support@fithub.com</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return await Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF content for invoice {InvoiceId}", invoice.Id);
            throw;
        }
    }

    /// <summary>
    /// Generate a formatted invoice number
    /// </summary>
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var lastInvoice = await _context.Invoices
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        var nextNumber = (lastInvoice?.Id ?? 0) + 1;
        return $"INV-{DateTime.UtcNow:yyyyMM}-{nextNumber:D6}";
    }
}


