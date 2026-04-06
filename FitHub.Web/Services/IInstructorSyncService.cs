using FitHub.Web.Models.Identity;

namespace FitHub.Web.Services;

public interface IInstructorSyncService
{
    Task<(bool Succeeded, string? ErrorMessage)> EnsureInstructorForUserAsync(ApplicationUser user);
}

