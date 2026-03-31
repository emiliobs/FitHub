using FitHub.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHub.Web.Models.Identity;

// This class MUST inherit from IdentityUser to be used in IdentityDbContext.
//  Inheriting ensures all security fields like Email and PasswordHash are included.
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "First name is mandatory")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only letters are allowed")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is mandatory")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    [RegularExpression(@"^[a-zA-Z\s]+$")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Profile Picture")]
    public string? Photo { get; set; } = "default-user.png";

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Member Since")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [Required]
    public SubscriptionType MembershipPlan { get; set; } = SubscriptionType.None;

    // Nullable because a 'None' user doesn't have an expiration date
    [DataType(DataType.Date)]
    public DateTime? SubscriptionEndDate { get; set; }

    // Navigation property for the relationship with Bookings (Already defined in your context)
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Helper method to check if the subscription is active based on the current date and subscription end date
    // Helper property to check if the subscription is currently active
    public bool IsSubscriptionActive =>
        MembershipPlan != SubscriptionType.None &&
        SubscriptionEndDate.HasValue &&
        SubscriptionEndDate.Value >= DateTime.UtcNow;
}