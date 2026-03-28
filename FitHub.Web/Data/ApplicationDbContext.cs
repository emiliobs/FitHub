using FitHub.Web.Models.Domain;
using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
            // M:N Relationship: Mapping Users and Classes through Bookings
            builder.Entity<Booking>()
                .HasOne(b => b.ApplicationUser)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.ApplicationUserId);

            builder.Entity<Booking>()
                .HasOne(b => b.FitnessClass)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.FitnessClassId);

            // SQL Server Decimal Precision for Currency (Price)
            builder.Entity<FitnessClass>()
                .Property(f => f.Price)
                .HasPrecision(18, 2);
        }
        catch (Exception ex)
        {
            // Catching errors during the database schema generation
            throw new InvalidOperationException($"Critical error: Database mapping failed: {ex.Message}");
        }
    }
}