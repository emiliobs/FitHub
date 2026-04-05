using FitHub.Web.Models.Domain;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First Name is required")]
    [Display(Name = "First Name")]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Numbers and special characters are not allowed")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required")]
    [Display(Name = "Last Name")]
    [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Numbers and special characters are not allowed")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public string? ExistingPhoto { get; set; }
    public IFormFile? PhotoFile { get; set; }

    // MEMBERSHIP STATUS DATA(Read-Only in View) ---

    [Display(Name = "Membership Plan")]
    public SubscriptionType MembershipPlan { get; set; }

    [Display(Name = "Expiration Date")]
    public DateTime? SubscriptionEndDate { get; set; }

    // Helper to calculate remaining days
    public int DaysRemaining => SubscriptionEndDate.HasValue
        ? (SubscriptionEndDate.Value - DateTime.UtcNow).Days
        : 0;

    public bool IsActive => MembershipPlan != SubscriptionType.None && DaysRemaining > 0;
}