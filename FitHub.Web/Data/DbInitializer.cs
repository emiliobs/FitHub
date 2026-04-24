using FitHub.Web.Models.Identity;
using FitHub.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Web.Data;

// The DbInitializer class is responsible for seeding the database with initial data.
public class DbInitializer
{
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        try
        {
            // We need RoleManager to seed roles (Admin, Manager, Instructor, Member)
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // We need both RoleManager and UserManager to seed roles and users
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // We also need the DbContext to seed categories, instructors, classes, and bookings
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. SEED ROLES: We create the 4 main roles for our application
            string[] roleNames = { "Admin", "Manager", "Instructor", "Member" };

            // We loop through the role names and create them if they don't already exist
            foreach (var roleName in roleNames)
            {
                //  Check if the role already exists to avoid duplicates
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    // If the role does not exist, we create it using the RoleManager
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. SEED USERS: Creating the 3 main Admin accounts
            var usersToSeed = new List<(string Email, string FirstName, string Role)>
            {
                ("admin@yopmail.com", "Admin", "Admin"),
                ("emilio@yopmail.com", "Emilio Barrera", "Admin"),
                ("kaur@yopmail.com", "Mehakdeep kaur", "Admin")
            };

            // We loop through the list of users to seed and create them if they don't already exist
            foreach (var userData in usersToSeed)
            {
                // Check if a user with the same email already exists to avoid duplicates
                var existingUser = await userManager.FindByEmailAsync(userData.Email);

                // If the user does not exist, we create a new ApplicationUser and assign the specified role
                if (existingUser == null)
                {
                    // We create a new ApplicationUser object with the provided email, first name, and role
                    var user = new ApplicationUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = "Barrera",
                        RegistrationDate = DateTime.UtcNow,
                        EmailConfirmed = true,
                        MembershipPlan = SubscriptionType.Warrior,
                        SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
                    };

                    // We create the user with a default password "123" (for development purposes only) and assign the specified role
                    await userManager.CreateAsync(user, "123");

                    // After creating the user, we assign them to the specified role using the UserManager
                    await userManager.AddToRoleAsync(user, userData.Role);
                }
            }

            // 3. SEED CATEGORIES
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Cardio" }, new Category { Name = "Weightlifting" },
                    new Category { Name = "Yoga" }, new Category { Name = "Crossfit" },
                    new Category { Name = "Zumba" }, new Category { Name = "Pilates" },
                    new Category { Name = "Spinning" }, new Category { Name = "Boxing" },
                    new Category { Name = "Swimming" }, new Category { Name = "Bodybuilding" },
                    new Category { Name = "HIIT" }, new Category { Name = "Functional Training" },
                    new Category { Name = "Martial Arts" }, new Category { Name = "Calisthenics" },
                    new Category { Name = "Flexibility" }, new Category { Name = "Powerlifting" },
                    new Category { Name = "Aerobics" }, new Category { Name = "Stretching" },
                    new Category { Name = "Strongman" }, new Category { Name = "Recovery" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. SEED INSTRUCTORS
            if (!context.Instructors.Any())
            {
                var dbCategories = await context.Categories.ToListAsync();
                var instructors = new List<Instructor>
                {
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

            // 5. SEED FITNESS CLASSES
            if (!context.FitnessClasses.Any())
            {
                var dbInstructors = await context.Instructors.ToListAsync();
                var dbCategories = await context.Categories.ToListAsync();

                var classes = new List<FitnessClass>
                {
                    new FitnessClass { Title = "Iron Morning", Description = "High intensity weightlifting", Capacity = 20, ScheduleDate = DateTime.Now.AddDays(1).AddHours(8), Price = 15.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Weightlifting").Id, InstructorId = dbInstructors.First(i => i.Name == "Chris Evans").Id },
                    new FitnessClass { Title = "Zen Flow", Description = "Sunrise yoga for flexibility", Capacity = 25, ScheduleDate = DateTime.Now.AddDays(1).AddHours(7), Price = 10.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Yoga").Id, InstructorId = dbInstructors.First(i => i.Name == "Elena Rodriguez").Id },
                    new FitnessClass { Title = "Warrior WOD", Description = "Official CrossFit training", Capacity = 15, ScheduleDate = DateTime.Now.AddDays(2).AddHours(18), Price = 20.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Crossfit").Id, InstructorId = dbInstructors.First(i => i.Name == "Marcus Thorne").Id },
                    new FitnessClass { Title = "Hyper HIIT", Description = "Burn calories fast", Capacity = 30, ScheduleDate = DateTime.Now.AddDays(1).AddHours(17), Price = 12.50m,
                        CategoryId = dbCategories.First(c => c.Name == "HIIT").Id, InstructorId = dbInstructors.First(i => i.Name == "Chloe Smith").Id },
                    new FitnessClass { Title = "Zumba Party", Description = "Dance and sweat", Capacity = 40, ScheduleDate = DateTime.Now.AddDays(3).AddHours(19), Price = 8.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Zumba").Id, InstructorId = dbInstructors.First(i => i.Name == "Sarah Jenkins").Id },
                    new FitnessClass { Title = "MMA Basics", Description = "Martial arts introduction", Capacity = 12, ScheduleDate = DateTime.Now.AddDays(2).AddHours(16), Price = 25.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Martial Arts").Id, InstructorId = dbInstructors.First(i => i.Name == "Tom Hardy").Id },
                    new FitnessClass { Title = "Power Pump", Description = "Bodybuilding fundamentals", Capacity = 20, ScheduleDate = DateTime.Now.AddDays(1).AddHours(15), Price = 18.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Bodybuilding").Id, InstructorId = dbInstructors.First(i => i.Name == "Robert Pattinson").Id },
                    new FitnessClass { Title = "Spinning Pro", Description = "High speed cycling", Capacity = 25, ScheduleDate = DateTime.Now.AddDays(1).AddHours(10), Price = 15.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Spinning").Id, InstructorId = dbInstructors.First(i => i.Name == "Maria Garcia").Id },
                    new FitnessClass { Title = "Titan Strength", Description = "Strongman specialized training", Capacity = 10, ScheduleDate = DateTime.Now.AddDays(4).AddHours(14), Price = 30.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Strongman").Id, InstructorId = dbInstructors.First(i => i.Name == "Henry Cavill").Id },
                    new FitnessClass { Title = "80s Aerobics", Description = "Retro workout fun", Capacity = 30, ScheduleDate = DateTime.Now.AddDays(4).AddHours(10), Price = 10.00m,
                        CategoryId = dbCategories.First(c => c.Name == "Aerobics").Id, InstructorId = dbInstructors.First(i => i.Name == "Margot Robbie").Id }
                };
                await context.FitnessClasses.AddRangeAsync(classes);
                await context.SaveChangesAsync();
            }

            // 6. BULK SEED BOOKINGS (This populates the Attendance Table)
            if (!context.Bookings.Any())
            {
                var admin = await userManager.FindByEmailAsync("admin@yopmail.com");
                var emilio = await userManager.FindByEmailAsync("emilio@yopmail.com");
                var kaur = await userManager.FindByEmailAsync("kaur@yopmail.com");

                // We enroll the 3 teammates in ALL classes generated above
                var allGymClasses = await context.FitnessClasses.ToListAsync();

                if (allGymClasses.Any())
                {
                    // We create a list of bookings to add in bulk for better performance
                    var bulkBookings = new List<Booking>();

                    // Each of the 3 users will be enrolled in all classes with different booking dates for variety
                    foreach (var fClass in allGymClasses)
                    {
                        if (admin != null)
                        {
                            bulkBookings.Add(new Booking { ApplicationUserId = admin.Id, FitnessClassId = fClass.Id, BookingDate = DateTime.UtcNow.AddHours(-5), PaidPrice = fClass.Price, Status = BookingStatus.Active, InternalNotes = "Seed Enrollment" });
                        }
                        if (emilio != null)
                        {
                            bulkBookings.Add(new Booking { ApplicationUserId = emilio.Id, FitnessClassId = fClass.Id, BookingDate = DateTime.UtcNow.AddHours(-3), PaidPrice = fClass.Price, Status = BookingStatus.Active, InternalNotes = "Seed Enrollment" });
                        }
                        if (kaur != null)
                        {
                            bulkBookings.Add(new Booking { ApplicationUserId = kaur.Id, FitnessClassId = fClass.Id, BookingDate = DateTime.UtcNow.AddHours(-1), PaidPrice = fClass.Price, Status = BookingStatus.Active, InternalNotes = "Seed Enrollment" });
                        }
                    }

                    // Add all bookings in bulk for better performance
                    await context.Bookings.AddRangeAsync(bulkBookings);

                    // Save all changes at once after adding all bookings
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error during database seeding: " + ex.Message, ex);
        }
    }
}