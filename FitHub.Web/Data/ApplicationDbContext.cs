using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;

namespace FitHub.Web.Data;

// The DbContext manages the database connection and entity mapping.
// We use IdentityDbContext to support Authentication and Authorization.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Domain Entities (DbSets) - Requirements for 3-tier architecture
    public DbSet<Category> Categories { get; set; }

    public DbSet<Instructor> Instructors { get; set; }
    public DbSet<FitnessClass> FitnessClasses { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Base configuration for Identity tables (Crucial for security)
        base.OnModelCreating(builder);

        try
        {
            // Custom configuration for ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.MembershipPlan).HasDefaultValue(SubscriptionType.None);
            });

            // M:N Relationship: Mapping Users and Classes through Bookings
            builder.Entity<Booking>()
                .HasOne(b => b.ApplicationUser)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.ApplicationUserId);

            builder.Entity<Booking>()
                .HasOne(b => b.FitnessClass)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.FitnessClassId);

            // <ake Instructor Email Unique to prevent duplicates and ensure data integrity
            builder.Entity<Instructor>()
                .HasIndex(i => i.Email)
                .IsUnique();

            // SQL Server Decimal Precision for Currency (Price)
            builder.Entity<FitnessClass>()
                .Property(f => f.Price)
                .HasPrecision(18, 2);

            // LOGIC: Configure the Foreign Key relationship between Instructor and Category
            // Configuración de la llave foránea entre Instructor y Categoría
            builder.Entity<Instructor>()
               .HasOne(i => i.Category)           // Each instructor belongs to one category
               .WithMany(c => c.Instructors)      // Each category has a list of instructors
               .HasForeignKey(i => i.CategoryId)  // Defining the Foreign Key property
               .OnDelete(DeleteBehavior.Restrict); // FIX: Prevents "Multiple Cascade Paths" error
        }
        catch (Exception ex)
        {
            // Catching errors during the database schema generation
            throw new InvalidOperationException($"Critical error: Database mapping failed: {ex.Message}");
        }
    }
}