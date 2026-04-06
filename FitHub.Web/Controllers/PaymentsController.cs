using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using FitHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

/// <summary>
/// Controller for managing payments and invoices
/// </summary>
[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Show payment history for the current user
    /// </summary>
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var payments = await _paymentService.GetPaymentHistoryAsync(user.Id);
        return View(payments);
    }

    /// <summary>
    /// Show payment details
    /// </summary>
    public async Task<IActionResult> Details(int paymentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .Include(p => p.Subscription)
            .ThenInclude(s => s.Category)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        return View(payment);
    }

    /// <summary>
    /// Retry a failed payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int paymentId, string? cardToken = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        if (payment.Status != PaymentStatus.Failed)
        {
            TempData["Info"] = "Only failed payments can be retried.";
            return RedirectToAction(nameof(Details), new { paymentId });
        }

        try
        {
            var paymentResult = await _paymentService.ProcessPaymentAsync(
                payment.Subscription,
                payment.PaymentMethod,
                cardToken);

            if (!paymentResult.IsSuccess)
            {
                TempData["Error"] = $"Payment retry failed: {paymentResult.ErrorMessage}";
                return RedirectToAction(nameof(Details), new { paymentId });
            }

            // Update the original payment status
            payment.Status = paymentResult.Payment!.Status;
            payment.TransactionId = paymentResult.Payment.TransactionId;
            payment.Notes = paymentResult.Payment.Notes;

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment processed successfully!";
            return RedirectToAction(nameof(Details), new { paymentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying payment {PaymentId}", paymentId);
            TempData["Error"] = "An error occurred during payment processing.";
            return RedirectToAction(nameof(Details), new { paymentId });
        }
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int paymentId, string? reason = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        try
        {
            var refundResult = await _paymentService.RefundPaymentAsync(payment, reason);

            if (!refundResult.IsSuccess)
            {
                TempData["Error"] = refundResult.ErrorMessage ?? "Refund failed.";
                return RedirectToAction(nameof(Details), new { paymentId });
            }

            TempData["Success"] = "Payment refunded successfully!";
            return RedirectToAction(nameof(History));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
            TempData["Error"] = "An error occurred during refund processing.";
            return RedirectToAction(nameof(Details), new { paymentId });
        }
    }

    /// <summary>
    /// Show all invoices for the current user
    /// </summary>
    public async Task<IActionResult> Invoices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var invoices = await _invoiceService.GetUserInvoicesAsync(user.Id);
        return View(invoices);
    }

    /// <summary>
    /// View invoice details
    /// </summary>
    public async Task<IActionResult> ViewInvoice(int invoiceId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);

        if (invoice == null || invoice.Payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        return View(invoice);
    }

    /// <summary>
    /// Download invoice as HTML
    /// </summary>
    public async Task<IActionResult> DownloadInvoice(int invoiceId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);

        if (invoice == null || invoice.Payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        try
        {
            var pdfContent = await _invoiceService.GeneratePdfContentAsync(invoice);
            var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);

            return File(bytes, "text/html", $"Invoice-{invoice.InvoiceNumber}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice {InvoiceId}", invoiceId);
            TempData["Error"] = "An error occurred while downloading the invoice.";
            return RedirectToAction(nameof(ViewInvoice), new { invoiceId });
        }
    }

    /// <summary>
    /// Admin endpoint to view all payments
    /// </summary>
    [Authorize(Policy = "CanManageBilling")]
    public async Task<IActionResult> AllPayments()
    {
        var payments = await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.ApplicationUser)
            .Include(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return View(payments);
    }

    /// <summary>
    /// Admin endpoint to view all invoices
    /// </summary>
    [Authorize(Policy = "CanManageBilling")]
    public async Task<IActionResult> AllInvoices()
    {
        var invoices = await _context.Invoices
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.ApplicationUser)
            .Include(i => i.Payment)
            .ThenInclude(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        return View(invoices);
    }
}


