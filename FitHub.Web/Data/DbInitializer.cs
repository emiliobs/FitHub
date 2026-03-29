using FitHub.Web.Models.Identity;
using FitHub.Web.Models.Domain; // Importante para Category
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

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

            // 1. Seed Roles
            string[] roleNames = { "Admin", "Trainer", "Member" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@yopmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Emilio",
                    LastName = "Barrera",
                    RegistrationDate = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, "Eabs123");
                await userManager.AddToRoleAsync(user, "Admin");
            }

            // 3. Seed Categories - Coursework Requirement (20 records)
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Cardio" },
                    new Category { Name = "Weightlifting" },
                    new Category { Name = "Yoga" },
                    new Category { Name = "Crossfit" },
                    new Category { Name = "Zumba" },
                    new Category { Name = "Pilates" },
                    new Category { Name = "Spinning" },
                    new Category { Name = "Boxing" },
                    new Category { Name = "Swimming" },
                    new Category { Name = "Bodybuilding" },
                    new Category { Name = "HIIT" },
                    new Category { Name = "Functional Training" },
                    new Category { Name = "Martial Arts" },
                    new Category { Name = "Calisthenics" },
                    new Category { Name = "Flexibility" },
                    new Category { Name = "Powerlifting" },
                    new Category { Name = "Aerobics" },
                    new Category { Name = "Stretching" },
                    new Category { Name = "Strongman" },
                    new Category { Name = "Recovery" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error during database seeding", ex);
        }
    }
}