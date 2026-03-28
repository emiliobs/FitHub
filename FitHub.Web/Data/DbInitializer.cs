using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace FitHub.Web.Data;

public class DbInitializer
{
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        try
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles - Coursework Requirement (Admin, Member, Trainer)
            string[] roleNames = { "Admin", "Trainer", "Member" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User - Ensuring Email is unique and matches UserName
            var adminEmail = "admin@fithub.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail, // Email and UserName are the same
                    Email = adminEmail,
                    FirstName = "Emilio",
                    LastName = "Barrera",
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "Admin123!");
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
        catch (Exception ex)
        {
            // Logic to handle seeding errors
            throw new Exception("Error during database seeding", ex);
        }
    }
}