using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using FitHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Controllers;

// Manages user payments, refunds, and invoices
[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentsController> _logger;

    // Inject required services
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

    // GET: Show user's payment history
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var payments = await _paymentService.GetPaymentHistoryAsync(user.Id);
        return View(payments);
    }

    // GET: Show details of a specific payment
    public async Task<IActionResult> Details(int paymentId)
    {
        // Ensure the payment belongs to the current user
        var user = await _userManager.GetUserAsync(User);

        // Block access if user is not authenticated
        if (user == null)
            return Unauthorized();

        // Include related subscription, fitness class, category, and invoices for detailed view
        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .Include(p => p.Subscription)
            .ThenInclude(s => s.Category)
            .Include(p => p.Invoices)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        // Block access if payment not found or does not belong to user
        if (payment == null || payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        return View(payment);
    }

    // POST: Retry a failed payment
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

        // Block retries for non-failed payments
        if (payment.Status != PaymentStatus.Failed)
        {
            TempData["Info"] = "Only failed payments can be retried.";
            return RedirectToAction(nameof(Details), new { paymentId });
        }

        try
        {
            // Attempt to process payment again
            var paymentResult = await _paymentService.ProcessPaymentAsync(
                payment.Subscription,
                payment.PaymentMethod,
                cardToken);

            if (!paymentResult.IsSuccess)
            {
                TempData["Error"] = $"Payment retry failed: {paymentResult.ErrorMessage}";
                return RedirectToAction(nameof(Details), new { paymentId });
            }

            // Update original payment record
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

    // POST: Process a payment refund
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int paymentId, string? reason = null)
    {
        // Ensure user is authenticated
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        // Ensure payment exists and belongs to user
        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        // Block access if payment not found or does not belong to user
        if (payment == null || payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        try
        {
            // Attempt to process refund
            var refundResult = await _paymentService.RefundPaymentAsync(payment, reason);

            // Block if refund fails
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
            // Log error and show generic message to user
            _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
            TempData["Error"] = "An error occurred during refund processing.";
            return RedirectToAction(nameof(Details), new { paymentId });
        }
    }

    // GET: Show user's invoices
    public async Task<IActionResult> Invoices()
    {
        // Ensure user is authenticated
        var user = await _userManager.GetUserAsync(User);

        // Block access if user is not authenticated
        if (user == null)
            return Unauthorized();

        // Retrieve invoices for the current user
        var invoices = await _invoiceService.GetUserInvoicesAsync(user.Id);
        return View(invoices);
    }

    // GET: Show invoice details
    public async Task<IActionResult> ViewInvoice(int invoiceId)
    {
        // Ensure user is authenticated
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        // Retrieve invoice and related payment/subscription details
        var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);

        // Ensure user owns the invoice
        if (invoice == null || invoice.Payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        return View(invoice);
    }

    // GET: Download invoice as an HTML file
    public async Task<IActionResult> DownloadInvoice(int invoiceId)
    {
        // Ensure user is authenticated
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        // Retrieve invoice and related payment/subscription details
        var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);

        // Ensure user owns the invoice
        if (invoice == null || invoice.Payment.Subscription.ApplicationUserId != user.Id)
            return NotFound();

        try
        {
            // Generate HTML content for the invoice
            var pdfContent = await _invoiceService.GeneratePdfContentAsync(invoice);

            // Convert HTML content to bytes for file download
            var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);

            // Return the invoice as a downloadable HTML file
            return File(bytes, "text/html", $"Invoice-{invoice.InvoiceNumber}.html");
        }
        catch (Exception ex)
        {
            // Log error and show generic message to user
            _logger.LogError(ex, "Error downloading invoice {InvoiceId}", invoiceId);
            TempData["Error"] = "An error occurred while downloading the invoice.";
            return RedirectToAction(nameof(ViewInvoice), new { invoiceId });
        }
    }

    // GET: Show all payments (Admin only)
    [Authorize(Policy = "CanManageBilling")]
    public async Task<IActionResult> AllPayments()
    {
        // Retrieve all payments with related subscription, user, and fitness class details for admin view
        var payments = await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.ApplicationUser)
            .Include(p => p.Subscription)
            .ThenInclude(s => s.FitnessClass)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return View(payments);
    }

    // GET: Show all invoices (Admin only)
    [Authorize(Policy = "CanManageBilling")]
    public async Task<IActionResult> AllInvoices()
    {
        // Retrieve all invoices with related payment, subscription, user, and fitness class details for admin view
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