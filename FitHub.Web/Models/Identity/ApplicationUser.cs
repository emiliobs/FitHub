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

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    // Navigation property
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}