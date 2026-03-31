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
            string[] roleNames = { "Admin", "Manager", "Instructor", "Member" };
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

                await userManager.CreateAsync(user, "123");
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

            // 4. Seed Instructors - Coursework Requirement (20 records)
            if (!context.Instructors.Any())
            {
                var instructors = new List<Instructor>
    {
        new Instructor { Name = "Marcus Thorne", Specialty = "CrossFit Expert", Email = "marcus@fithub.com", Phone = "07712345671", Photo = "default-user.png" },
        new Instructor { Name = "Elena Rodriguez", Specialty = "Yoga & Pilates", Email = "elena@fithub.com", Phone = "07712345672", Photo = "default-user.png" },
        new Instructor { Name = "Sarah Jenkins", Specialty = "Zumba Instructor", Email = "sarah@fithub.com", Phone = "07712345673", Photo = "default-user.png" },
        new Instructor { Name = "David Beckham", Specialty = "Football Conditioning", Email = "david@fithub.com", Phone = "07712345674", Photo = "default-user.png" },
        new Instructor { Name = "Chloe Smith", Specialty = "HIIT Specialist", Email = "chloe@fithub.com", Phone = "07712345675", Photo = "default-user.png" },
        new Instructor { Name = "James Bond", Specialty = "Self Defense", Email = "007@fithub.com", Phone = "07712345676", Photo = "default-user.png" },
        new Instructor { Name = "Maria Garcia", Specialty = "Spinning Pro", Email = "maria@fithub.com", Phone = "07712345677", Photo = "default-user.png" },
        new Instructor { Name = "Robert Pattinson", Specialty = "Bodybuilding", Email = "robert@fithub.com", Phone = "07712345678", Photo = "default-user.png" },
        new Instructor { Name = "Emma Watson", Specialty = "Flexibility Coach", Email = "emma@fithub.com", Phone = "07712345679", Photo = "default-user.png" },
        new Instructor { Name = "Chris Evans", Specialty = "Strength & Core", Email = "chris@fithub.com", Phone = "07712345680", Photo = "default-user.png" },
        new Instructor { Name = "Scarlett J.", Specialty = "Kickboxing", Email = "scarlett@fithub.com", Phone = "07712345681", Photo = "default-user.png" },
        new Instructor { Name = "Tom Hardy", Specialty = "MMA Trainer", Email = "tom@fithub.com", Phone = "07712345682", Photo = "default-user.png" },
        new Instructor { Name = "Gal Gadot", Specialty = "Functional Training", Email = "gal@fithub.com", Phone = "07712345683", Photo = "default-user.png" },
        new Instructor { Name = "Henry Cavill", Specialty = "Heavy Lifting", Email = "henry@fithub.com", Phone = "07712345684", Photo = "default-user.png" },
        new Instructor { Name = "Margot Robbie", Specialty = "Aerobics", Email = "margot@fithub.com", Phone = "07712345685", Photo = "default-user.png" },
        new Instructor { Name = "Dwayne Johnson", Specialty = "Powerlifting", Email = "theock@fithub.com", Phone = "07712345686", Photo = "default-user.png" },
        new Instructor { Name = "Jason Momoa", Specialty = "Swimming Fitness", Email = "jason@fithub.com", Phone = "07712345687", Photo = "default-user.png" },
        new Instructor { Name = "Brie Larson", Specialty = "Cardio Dance", Email = "brie@fithub.com", Phone = "07712345688", Photo = "default-user.png" },
        new Instructor { Name = "Tom Holland", Specialty = "Gymnastics", Email = "spider@fithub.com", Phone = "07712345689", Photo = "default-user.png" },
        new Instructor { Name = "Zendaya Coleman", Specialty = "Contemporary Dance", Email = "zen@fithub.com", Phone = "07712345690", Photo = "default-user.png" }
    };

                await context.Instructors.AddRangeAsync(instructors);
                await context.SaveChangesAsync();
            }

            // 5. Seed FitnessClasses - Sample classes
            if (!context.FitnessClasses.Any())
            {
                var fitnessClasses = new List<FitnessClass>
                {
                    new FitnessClass { Title = "Morning Yoga Flow", Description = "Start your day with energizing yoga poses", Capacity = 20, ScheduleDate = DateTime.UtcNow.AddDays(1).AddHours(8), Price = 15.00m, InstructorId = 2, CategoryId = 3 },
                    new FitnessClass { Title = "HIIT Blast", Description = "High-intensity interval training for fat burn", Capacity = 15, ScheduleDate = DateTime.UtcNow.AddDays(2).AddHours(18), Price = 20.00m, InstructorId = 5, CategoryId = 11 },
                    new FitnessClass { Title = "CrossFit Fundamentals", Description = "Learn the basics of CrossFit", Capacity = 12, ScheduleDate = DateTime.UtcNow.AddDays(3).AddHours(10), Price = 25.00m, InstructorId = 1, CategoryId = 4 },
                    new FitnessClass { Title = "Zumba Dance Party", Description = "Fun dance workout to upbeat music", Capacity = 25, ScheduleDate = DateTime.UtcNow.AddDays(4).AddHours(19), Price = 12.00m, InstructorId = 3, CategoryId = 5 },
                    new FitnessClass { Title = "Strength Training 101", Description = "Build muscle with basic strength exercises", Capacity = 10, ScheduleDate = DateTime.UtcNow.AddDays(5).AddHours(9), Price = 18.00m, InstructorId = 10, CategoryId = 2 }
                };

                await context.FitnessClasses.AddRangeAsync(fitnessClasses);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error during database seeding", ex);
        }
    }
}