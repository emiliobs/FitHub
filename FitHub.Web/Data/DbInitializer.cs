using FitHub.Web.Models.Identity;
using FitHub.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Data;

public class DbInitializer
{
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        try
        {
            // Getting necessary services from the DI container
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. SEED ROLES: Ensuring basic security roles exist
            string[] roleNames = { "Admin", "Manager", "Instructor", "Member" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. SEED ADMIN USER: Creating the initial administrator
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
                    EmailConfirmed = true,
                    MembershipPlan = SubscriptionType.Warrior, // granting the highest subscription level to the admin for testing purposes
                    SubscriptionEndDate = DateTime.UtcNow.AddYears(10) // setting a far future date to ensure the admin's subscription is always active
                };

                // Using a simple password for initial setup
                await userManager.CreateAsync(user, "123");
                await userManager.AddToRoleAsync(user, "Admin");
            }

            // 3. SEED CATEGORIES: Required for the Instructor relationship
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Cardio" },            // ID 1
                    new Category { Name = "Weightlifting" },     // ID 2
                    new Category { Name = "Yoga" },              // ID 3
                    new Category { Name = "Crossfit" },          // ID 4
                    new Category { Name = "Zumba" },             // ID 5
                    new Category { Name = "Pilates" },           // ID 6
                    new Category { Name = "Spinning" },          // ID 7
                    new Category { Name = "Boxing" },            // ID 8
                    new Category { Name = "Swimming" },          // ID 9
                    new Category { Name = "Bodybuilding" },      // ID 10
                    new Category { Name = "HIIT" },              // ID 11
                    new Category { Name = "Functional Training" },// ID 12
                    new Category { Name = "Martial Arts" },      // ID 13
                    new Category { Name = "Calisthenics" },      // ID 14
                    new Category { Name = "Flexibility" },       // ID 15
                    new Category { Name = "Powerlifting" },      // ID 16
                    new Category { Name = "Aerobics" },          // ID 17
                    new Category { Name = "Stretching" },        // ID 18
                    new Category { Name = "Strongman" },         // ID 19
                    new Category { Name = "Recovery" }           // ID 20
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. SEED INSTRUCTORS: Connecting them to existing Categories
            if (!context.Instructors.Any())
            {
                // Fetch categories from DB to get their generated IDs
                var dbCategories = await context.Categories.ToListAsync();

                var instructors = new List<Instructor>
                {
                    // We map each instructor to a CategoryId instead of a plain text specialty
                    new Instructor { Name = "Marcus Thorne", CategoryId = dbCategories.First(c => c.Name == "Crossfit").Id, Email = "marcus@fithub.com", Phone = "07712345671", Photo = "default-user.png" },
                    new Instructor { Name = "Elena Rodriguez", CategoryId = dbCategories.First(c => c.Name == "Yoga").Id, Email = "elena@fithub.com", Phone = "07712345672", Photo = "default-user.png" },
                    new Instructor { Name = "Sarah Jenkins", CategoryId = dbCategories.First(c => c.Name == "Zumba").Id, Email = "sarah@fithub.com", Phone = "07712345673", Photo = "default-user.png" },
                    new Instructor { Name = "David Beckham", CategoryId = dbCategories.First(c => c.Name == "Cardio").Id, Email = "david@fithub.com", Phone = "07712345674", Photo = "default-user.png" },
                    new Instructor { Name = "Chloe Smith", CategoryId = dbCategories.First(c => c.Name == "HIIT").Id, Email = "chloe@fithub.com", Phone = "07712345675", Photo = "default-user.png" },
                    new Instructor { Name = "James Bond", CategoryId = dbCategories.First(c => c.Name == "Martial Arts").Id, Email = "007@fithub.com", Phone = "07712345676", Photo = "default-user.png" },
                    new Instructor { Name = "Maria Garcia", CategoryId = dbCategories.First(c => c.Name == "Spinning").Id, Email = "maria@fithub.com", Phone = "07712345677", Photo = "default-user.png" },
                    new Instructor { Name = "Robert Pattinson", CategoryId = dbCategories.First(c => c.Name == "Bodybuilding").Id, Email = "robert@fithub.com", Phone = "07712345678", Photo = "default-user.png" },
                    new Instructor { Name = "Emma Watson", CategoryId = dbCategories.First(c => c.Name == "Flexibility").Id, Email = "emma@fithub.com", Phone = "07712345679", Photo = "default-user.png" },
                    new Instructor { Name = "Chris Evans", CategoryId = dbCategories.First(c => c.Name == "Weightlifting").Id, Email = "chris@fithub.com", Phone = "07712345680", Photo = "default-user.png" },
                    new Instructor { Name = "Scarlett J.", CategoryId = dbCategories.First(c => c.Name == "Boxing").Id, Email = "scarlett@fithub.com", Phone = "07712345681", Photo = "default-user.png" },
                    new Instructor { Name = "Tom Hardy", CategoryId = dbCategories.First(c => c.Name == "Martial Arts").Id, Email = "tom@fithub.com", Phone = "07712345682", Photo = "default-user.png" },
                    new Instructor { Name = "Gal Gadot", CategoryId = dbCategories.First(c => c.Name == "Functional Training").Id, Email = "gal@fithub.com", Phone = "07712345683", Photo = "default-user.png" },
                    new Instructor { Name = "Henry Cavill", CategoryId = dbCategories.First(c => c.Name == "Strongman").Id, Email = "henry@fithub.com", Phone = "07712345684", Photo = "default-user.png" },
                    new Instructor { Name = "Margot Robbie", CategoryId = dbCategories.First(c => c.Name == "Aerobics").Id, Email = "margot@fithub.com", Phone = "07712345685", Photo = "default-user.png" },
                    new Instructor { Name = "Dwayne Johnson", CategoryId = dbCategories.First(c => c.Name == "Powerlifting").Id, Email = "theock@fithub.com", Phone = "07712345686", Photo = "default-user.png" },
                    new Instructor { Name = "Jason Momoa", CategoryId = dbCategories.First(c => c.Name == "Swimming").Id, Email = "jason@fithub.com", Phone = "07712345687", Photo = "default-user.png" },
                    new Instructor { Name = "Brie Larson", CategoryId = dbCategories.First(c => c.Name == "Recovery").Id, Email = "brie@fithub.com", Phone = "07712345688", Photo = "default-user.png" },
                    new Instructor { Name = "Tom Holland", CategoryId = dbCategories.First(c => c.Name == "Calisthenics").Id, Email = "spider@fithub.com", Phone = "07712345689", Photo = "default-user.png" },
                    new Instructor { Name = "Zendaya Coleman", CategoryId = dbCategories.First(c => c.Name == "Stretching").Id, Email = "zen@fithub.com", Phone = "07712345690", Photo = "default-user.png" }
                };

                await context.Instructors.AddRangeAsync(instructors);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Logging the exception for debugging purposes
            throw new Exception("Error during database seeding: " + ex.Message, ex);
        }
    }
}