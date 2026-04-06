namespace FitHub.Web.ViewModels;

public class ReserveCheckoutViewModel
{
    public int FitnessClassId { get; set; }
    public string? CardToken { get; set; }
    public string ClassTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime ScheduleDate { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int ActiveBookings { get; set; }
    public bool IsFreeAccess { get; set; }
}

