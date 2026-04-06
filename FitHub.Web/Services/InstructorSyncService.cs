using FitHub.Web.Data;
using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Services;

public class InstructorSyncService : IInstructorSyncService
{
    private const string DefaultProfilePhoto = "default-user.png";
    private const string DefaultInstructorPhone = "07000000000";

    private readonly ApplicationDbContext _context;

    public InstructorSyncService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> EnsureInstructorForUserAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return (false, "Cannot sync instructor profile because user email is missing.");
        }

        var instructor = await _context.Instructors
            .FirstOrDefaultAsync(i => i.Email == user.Email);

        if (instructor == null)
        {
            var fallbackCategory = await _context.Categories
                .OrderBy(c => c.Name)
                .FirstOrDefaultAsync();

            if (fallbackCategory == null)
            {
                return (false, "Cannot create instructor profile because no categories exist.");
            }

            instructor = new Instructor
            {
                Name = BuildDisplayName(user),
                Email = user.Email,
                Phone = DefaultInstructorPhone,
                Photo = string.IsNullOrWhiteSpace(user.Photo) ? DefaultProfilePhoto : user.Photo,
                CategoryId = fallbackCategory.Id
            };

            _context.Instructors.Add(instructor);
        }
        else
        {
            instructor.Name = BuildDisplayName(user);
            instructor.Photo = string.IsNullOrWhiteSpace(user.Photo) ? DefaultProfilePhoto : user.Photo;
            instructor.Email = user.Email;

            if (string.IsNullOrWhiteSpace(instructor.Phone))
            {
                instructor.Phone = DefaultInstructorPhone;
            }
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    private static string BuildDisplayName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Email ?? "Instructor" : name;
    }
}

