using FitHub.Web.Models.Domain;
using FitHub.Web.ViewModels;
using FitHub.Web.Services;
using FitHub.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitHub.Web.Controllers;

// Requires users to be logged in to access bookings
[Authorize]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<BookingsController> _logger;

    // Inject required services (Database, Payments, Invoices, Logging)
    public BookingsController(
        ApplicationDbContext context,
        IPaymentService paymentService,
        IInvoiceService invoiceService,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Fallback redirect to the main classes page
        return RedirectToAction("Index", "FitnessClasses");
    }

    // GET : Bookings/MySchedule
    [HttpGet]
    public async Task<IActionResult> MySchedule()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var instructor = await GetCurrentInstructorAsync(userId);

        try
        {
            // If user is an instructor, show the instructor dashboard
            if (instructor != null)
            {
                var instructorSchedule = await BuildInstructorScheduleViewModelAsync(instructor);
                return View("InstructorSchedule", instructorSchedule);
            }

            // Otherwise, load the regular member's active and completed bookings
            var myBookings = await _context.Bookings
                .Include(b => b.FitnessClass)
                .ThenInclude(c => c.Instructor)
                .Include(b => b.FitnessClass.Category)
                .Include(b => b.BookingCheckIn)
                .Where(b => b.ApplicationUserId == userId &&
                            (b.Status == BookingStatus.Active || b.Status == BookingStatus.Completed))
                .OrderBy(b => b.FitnessClass.ScheduleDate)
                .ToListAsync();

            return View(myBookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schedule data");
            TempData["Error"] = GetFriendlyScheduleErrorMessage(ex);

            // Return safe fallback views on error
            if (instructor != null)
            {
                return View("InstructorSchedule", new InstructorScheduleViewModel
                {
                    InstructorName = instructor.Name,
                    InstructorEmail = instructor.Email ?? string.Empty
                });
            }

            return View(Array.Empty<Booking>());
        }
    }

    // GET: Show checkout/reservation page
    [HttpGet]
    public async Task<IActionResult> Reserve(int classId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var fitnessClass = await _context.FitnessClasses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (fitnessClass == null)
        {
            TempData["Error"] = "Class not found.";
            return RedirectToAction("Index", "FitnessClasses");
        }

        // Prevent duplicate bookings
        var alreadyBooked = await _context.Bookings.AnyAsync(b =>
            b.ApplicationUserId == userId &&
            b.FitnessClassId == classId &&
            b.Status == BookingStatus.Active);

        if (alreadyBooked)
        {
            TempData["Info"] = "You have already reserved this class.";
            return RedirectToAction("Index", "FitnessClasses");
        }

        // Prevent booking if class is full
        var activeSeats = await _context.Bookings.CountAsync(b => b.FitnessClassId == classId && b.Status == BookingStatus.Active);
        if (activeSeats >= fitnessClass.Capacity)
        {
            TempData["Error"] = "This class is fully booked.";
            return RedirectToAction("Index", "FitnessClasses");
        }

        var viewModel = new ReserveCheckoutViewModel
        {
            FitnessClassId = fitnessClass.Id,
            ClassTitle = fitnessClass.Title,
            CategoryName = fitnessClass.Category?.Name ?? "General",
            InstructorName = fitnessClass.Instructor?.Name ?? "Unknown",
            ScheduleDate = fitnessClass.ScheduleDate,
            Price = fitnessClass.Price,
            Capacity = fitnessClass.Capacity,
            ActiveBookings = activeSeats,
            IsFreeAccess = await CanJoinClassForFreeAsync(userId, fitnessClass)
        };

        return View(viewModel);
    }

    // POST: Process the reservation
    [HttpPost]
    public async Task<IActionResult> Reserve(ReserveCheckoutViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var fitnessClass = await _context.FitnessClasses
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == model.FitnessClassId);

        if (fitnessClass == null)
        {
            TempData["Error"] = "Class not found.";
            return RedirectToAction("Index", "FitnessClasses");
        }

        // Bypass payment if the class is free or user is an instructor
        if (await CanJoinClassForFreeAsync(userId, fitnessClass))
        {
            return await CreateFreeReservationAsync(userId, fitnessClass);
        }

        // Setup Stripe Checkout Gateway for paid classes
        var successPath = Url.Action(nameof(ReserveSuccess), "Bookings", new { classId = model.FitnessClassId }, Request.Scheme);
        var cancelPath = Url.Action(nameof(ReserveCancelled), "Bookings", new { classId = model.FitnessClassId }, Request.Scheme);

        if (string.IsNullOrWhiteSpace(successPath) || string.IsNullOrWhiteSpace(cancelPath))
        {
            TempData["Error"] = "Could not initialize payment gateway URLs.";
            return RedirectToAction(nameof(Reserve), new { classId = model.FitnessClassId });
        }

        var successUrl = $"{successPath}&sessionId={{CHECKOUT_SESSION_ID}}";
        var stripeSession = await _paymentService.CreateStripeCheckoutSessionAsync(
            userId, fitnessClass.Id, fitnessClass.Title, fitnessClass.Price, successUrl, cancelPath);

        if (!stripeSession.IsSuccess || string.IsNullOrWhiteSpace(stripeSession.CheckoutUrl))
        {
            TempData["Error"] = stripeSession.ErrorMessage ?? "Could not initialize secure payment gateway.";
            return RedirectToAction(nameof(Reserve), new { classId = model.FitnessClassId });
        }

        // Redirect user to Stripe secure payment page
        return Redirect(stripeSession.CheckoutUrl);
    }

    // GET: User cancelled Stripe payment
    [HttpGet]
    public IActionResult ReserveCancelled(int classId)
    {
        TempData["Info"] = "Payment was cancelled. You can retry reservation when ready.";
        return RedirectToAction(nameof(Reserve), new { classId });
    }

    // GET: Successful Stripe payment callback
    [HttpGet]
    public async Task<IActionResult> ReserveSuccess(int classId, string sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            TempData["Error"] = "Missing payment session confirmation.";
            return RedirectToAction(nameof(Reserve), new { classId });
        }

        // Verify payment is authentic
        var verification = await _paymentService.VerifyStripeCheckoutPaymentAsync(sessionId, userId, classId);
        if (!verification.IsSuccess)
        {
            TempData["Error"] = verification.ErrorMessage ?? "Payment verification failed.";
            return RedirectToAction(nameof(Reserve), new { classId });
        }

        var alreadyBooked = await _context.Bookings.AnyAsync(b =>
            b.ApplicationUserId == userId && b.FitnessClassId == classId && b.Status != BookingStatus.Cancelled);

        if (alreadyBooked)
        {
            TempData["Info"] = "This class is already in your schedule.";
            return RedirectToAction(nameof(MySchedule));
        }

        var fitnessClass = await _context.FitnessClasses
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (fitnessClass == null)
        {
            TempData["Error"] = "Class not found.";
            return RedirectToAction("Index", "FitnessClasses");
        }

        // Create Database Transaction to ensure all related records save together safely
        using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Create Subscription
            var classSubscription = new Subscription
            {
                ApplicationUserId = userId,
                FitnessClassId = fitnessClass.Id,
                CategoryId = fitnessClass.CategoryId,
                StartDate = DateTime.UtcNow,
                EndDate = fitnessClass.ScheduleDate.AddDays(1),
                AutoRenew = false,
                IsActive = true,
                PriceAtPurchase = fitnessClass.Price,
                CreatedDate = DateTime.UtcNow
            };
            _context.Subscriptions.Add(classSubscription);
            await _context.SaveChangesAsync();

            // 2. Create Payment Record
            var payment = new Payment
            {
                SubscriptionId = classSubscription.Id,
                Amount = fitnessClass.Price,
                PaymentMethod = "Stripe",
                Status = PaymentStatus.Completed,
                TransactionId = verification.TransactionId,
                PaymentDate = DateTime.UtcNow,
                Notes = $"Stripe checkout session {sessionId}"
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // 3. Generate Invoice
            var invoice = await _invoiceService.GenerateInvoiceAsync(payment);

            // 4. Create Final Booking Record
            var booking = new Booking
            {
                ApplicationUserId = userId,
                FitnessClassId = classId,
                SubscriptionId = classSubscription.Id,
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                BookingDate = DateTime.UtcNow,
                PaidPrice = fitnessClass.Price,
                Status = BookingStatus.Active,
                InternalNotes = $"Booked with Stripe transaction {verification.TransactionId}"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Reserved successfully. Transaction: {verification.TransactionId}, Invoice: {invoice.InvoiceNumber}.";
            return RedirectToAction(nameof(MySchedule));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error finalizing Stripe reservation");
            TempData["Error"] = "Payment was captured, but reservation finalization failed. Please contact support with your transaction reference.";
            return RedirectToAction(nameof(MySchedule));
        }
    }

    // POST: Cancel an active booking
    [HttpPost]
    public async Task<IActionResult> Cancel(int bookingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.ApplicationUserId == userId);

        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (booking.Status != BookingStatus.Active)
        {
            TempData["Error"] = "Only active reservations can be cancelled.";
            return RedirectToAction(nameof(MySchedule));
        }

        // Change status to Cancelled
        booking.Status = BookingStatus.Cancelled;
        booking.InternalNotes = "Cancelled by user";

        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Reservation cancelled successfully.";
        return RedirectToAction(nameof(MySchedule));
    }

    // GET: Generate QR Code for Class Check-in
    [HttpGet]
    public async Task<IActionResult> CheckIn(int bookingId, string? browserTimeZone = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.FitnessClass)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ApplicationUserId == userId);

        if (booking == null)
        {
            TempData["Error"] = "Booking not found.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (booking.Status == BookingStatus.Completed)
        {
            TempData["Error"] = "You have already checked in for this class.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (booking.Status != BookingStatus.Active)
        {
            TempData["Error"] = "This booking is not eligible for check-in.";
            return RedirectToAction(nameof(MySchedule));
        }

        // Validate if we are within the 1-hour window before class starts
        var canCheckIn = CanOpenCheckInWindow(booking.FitnessClass.ScheduleDate, browserTimeZone, out var openWindowText, out var nowText);
        if (!canCheckIn)
        {
            TempData["Error"] = $"QR code will be visible 1 hour before the class. Check-in opens at {openWindowText}. Current time: {nowText}.";
            return RedirectToAction(nameof(MySchedule));
        }

        var existingCheckIn = await _context.BookingCheckIns.FirstOrDefaultAsync(ci => ci.BookingId == booking.Id);

        if (existingCheckIn?.IsRedeemed == true)
        {
            TempData["Error"] = "Check-in already redeemed for this enrollment.";
            return RedirectToAction(nameof(MySchedule));
        }

        // Create new QR check-in token if it doesn't exist
        if (existingCheckIn == null)
        {
            existingCheckIn = new BookingCheckIn
            {
                BookingId = booking.Id,
                ApplicationUserId = booking.ApplicationUserId,
                FitnessClassId = booking.FitnessClassId,
                EnrollmentReference = booking.EnrollmentReference,
                QrToken = Guid.NewGuid().ToString("N"),
                GeneratedAtUtc = DateTime.UtcNow,
                IsRedeemed = false
            };

            _context.BookingCheckIns.Add(existingCheckIn);
            await _context.SaveChangesAsync();
        }

        // Generate QR code URL
        var redeemUrl = Url.Action(nameof(RedeemCheckIn), "Bookings", new { token = existingCheckIn.QrToken }, Request.Scheme);
        if (string.IsNullOrWhiteSpace(redeemUrl))
        {
            TempData["Error"] = "Could not generate check-in QR code at the moment.";
            return RedirectToAction(nameof(MySchedule));
        }

        var viewModel = new BookingCheckInViewModel
        {
            BookingId = booking.Id,
            EnrollmentReference = booking.EnrollmentReference,
            ClassTitle = booking.FitnessClass.Title,
            ScheduleDateUtc = booking.FitnessClass.ScheduleDate,
            RedeemUrl = redeemUrl,
            QrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=320x320&data={Uri.EscapeDataString(redeemUrl)}"
        };

        return View(viewModel);
    }

    // GET: Scan/Redeem the QR Code
    [HttpGet]
    public async Task<IActionResult> RedeemCheckIn(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Invalid check-in request.";
            return RedirectToAction(nameof(MySchedule));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var checkIn = await _context.BookingCheckIns
            .Include(ci => ci.Booking)
            .Include(ci => ci.FitnessClass)
            .FirstOrDefaultAsync(ci => ci.QrToken == token);

        if (checkIn == null)
        {
            TempData["Error"] = "Check-in token is invalid or expired.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (checkIn.ApplicationUserId != userId)
        {
            TempData["Error"] = "You are not authorized to redeem this check-in.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (checkIn.IsRedeemed)
        {
            TempData["Error"] = "This class has already been marked present.";
            return RedirectToAction(nameof(MySchedule));
        }

        if (!CanOpenCheckInWindow(checkIn.FitnessClass.ScheduleDate, null, out _, out _))
        {
            TempData["Error"] = "This QR cannot be redeemed yet. It becomes valid 1 hour before class starts.";
            return RedirectToAction(nameof(MySchedule));
        }

        // Mark attendance as completed
        checkIn.IsRedeemed = true;
        checkIn.RedeemedAtUtc = DateTime.UtcNow;
        checkIn.Booking.Status = BookingStatus.Completed;
        checkIn.Booking.InternalNotes = $"Attendance redeemed via QR at {checkIn.RedeemedAtUtc:yyyy-MM-dd HH:mm:ss} UTC";

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Check-in successful. Enrollment {checkIn.EnrollmentReference} marked present.";
        return RedirectToAction(nameof(MySchedule));
    }

    // --- Helper Methods Below ---

    // Helper: Validates if the current time is within 1 hour before the class starts
    private static bool CanOpenCheckInWindow(DateTime classScheduleUtc, string? browserTimeZone, out string openWindowText, out string currentWindowText)
    {
        var currentUtc = DateTime.UtcNow;
        var openUtc = classScheduleUtc.AddHours(-1);

        if (!string.IsNullOrWhiteSpace(browserTimeZone))
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(browserTimeZone);
                var currentLocal = TimeZoneInfo.ConvertTimeFromUtc(currentUtc, tz);
                var openLocal = TimeZoneInfo.ConvertTimeFromUtc(openUtc, tz);
                openWindowText = $"{openLocal:yyyy-MM-dd hh:mm tt} {browserTimeZone}";
                currentWindowText = $"{currentLocal:yyyy-MM-dd hh:mm tt} {browserTimeZone}";
                return currentLocal >= openLocal;
            }
            catch
            {
                // Fall back to UTC formatting on error
            }
        }

        openWindowText = $"{openUtc:yyyy-MM-dd HH:mm} UTC";
        currentWindowText = $"{currentUtc:yyyy-MM-dd HH:mm} UTC";
        return currentUtc >= openUtc;
    }

    // Helper: Gets the instructor profile linked to the logged-in user
    private async Task<Instructor?> GetCurrentInstructorAsync(string userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email == null) return null;

        return await _context.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.Email == user.Email);
    }

    // Helper: Checks if the user skips payment (price is 0 or user is staff)
    private async Task<bool> CanJoinClassForFreeAsync(string userId, FitnessClass fitnessClass)
    {
        return fitnessClass.Price <= 0m || await GetCurrentInstructorAsync(userId) != null;
    }

    // Helper: Processes a free booking without contacting Stripe
    private async Task<IActionResult> CreateFreeReservationAsync(string userId, FitnessClass fitnessClass)
    {
        using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var booking = new Booking
            {
                ApplicationUserId = userId,
                FitnessClassId = fitnessClass.Id,
                BookingDate = DateTime.UtcNow,
                PaidPrice = 0m,
                Status = BookingStatus.Active,
                InternalNotes = fitnessClass.Price <= 0m ? "Joined free class" : "Joined free as instructor participant"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "You joined the class as a participant at no cost.";
            return RedirectToAction(nameof(MySchedule));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Error creating free instructor reservation");
            TempData["Error"] = "We couldn't complete the free class join right now. Please try again.";
            return RedirectToAction("Index", "FitnessClasses");
        }
    }

    // Helper: Gathers data for the Instructor's dashboard view
    private async Task<InstructorScheduleViewModel> BuildInstructorScheduleViewModelAsync(Instructor instructor)
    {
        var classes = await _context.FitnessClasses
            .Include(c => c.Category)
            .Include(c => c.Bookings).ThenInclude(b => b.ApplicationUser)
            .Include(c => c.Bookings).ThenInclude(b => b.BookingCheckIn)
            .Where(c => c.InstructorId == instructor.Id)
            .OrderBy(c => c.ScheduleDate)
            .ToListAsync();

        return new InstructorScheduleViewModel
        {
            InstructorName = instructor.Name,
            InstructorEmail = instructor.Email ?? string.Empty,
            Classes = classes.Select(fitnessClass => new InstructorScheduleClassViewModel
            {
                FitnessClassId = fitnessClass.Id,
                Title = fitnessClass.Title,
                CategoryName = fitnessClass.Category?.Name ?? "General",
                ScheduleDate = fitnessClass.ScheduleDate,
                Capacity = fitnessClass.Capacity,
                Price = fitnessClass.Price,
                ParticipantCount = fitnessClass.Bookings.Count(b => b.Status == BookingStatus.Active || b.Status == BookingStatus.Completed),
                Participants = fitnessClass.Bookings
                    .Where(b => b.Status == BookingStatus.Active || b.Status == BookingStatus.Completed)
                    .OrderBy(b => b.BookingDate)
                    .Select(b => new InstructorScheduleParticipantViewModel
                    {
                        BookingId = b.Id,
                        FullName = b.ApplicationUser.FullName,
                        Email = b.ApplicationUser.Email ?? string.Empty,
                        BookingDate = b.BookingDate,
                        Status = b.Status,
                        PaidPrice = b.PaidPrice,
                        IsCheckedIn = b.BookingCheckIn?.IsRedeemed == true,
                        EnrollmentReference = b.EnrollmentReference
                    }).ToList()
            }).ToList()
        };
    }

    // Helper: Formats user-friendly error messages based on exception types
    private static string GetFriendlyScheduleErrorMessage(Exception ex)
    {
        var message = ex.GetBaseException().Message;

        if (message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
        {
            return "Your training schedule is temporarily unavailable because the database schema still needs to be updated for the new class-based booking flow. Please try again after the database migration is applied.";
        }

        return "We couldn't load your training schedule right now. Please try again in a moment.";
    }
}