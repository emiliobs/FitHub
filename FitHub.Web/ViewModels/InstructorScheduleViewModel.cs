using FitHub.Web.Models.Domain;

namespace FitHub.Web.ViewModels;

public class InstructorScheduleViewModel
{
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorEmail { get; set; } = string.Empty;
    public IList<InstructorScheduleClassViewModel> Classes { get; set; } = new List<InstructorScheduleClassViewModel>();
}

public class InstructorScheduleClassViewModel
{
    public int FitnessClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime ScheduleDate { get; set; }
    public int Capacity { get; set; }
    public decimal Price { get; set; }
    public int ParticipantCount { get; set; }
    public IList<InstructorScheduleParticipantViewModel> Participants { get; set; } = new List<InstructorScheduleParticipantViewModel>();
}

public class InstructorScheduleParticipantViewModel
{
    public int BookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public BookingStatus Status { get; set; }
    public decimal PaidPrice { get; set; }
    public bool IsCheckedIn { get; set; }
    public string EnrollmentReference { get; set; } = string.Empty;
}

