namespace FitHub.Web.ViewModels;

public class BookingCheckInViewModel
{
    public int BookingId { get; set; }
    public string EnrollmentReference { get; set; } = string.Empty;
    public string ClassTitle { get; set; } = string.Empty;
    public DateTime ScheduleDateUtc { get; set; }
    public string RedeemUrl { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
}

