using FitHub.Web.Data;
using FitHub.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server Connection
try
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}
catch (Exception ex)
{
    // Log database configuration errors
    Console.WriteLine($"Database Setup Error: {ex.Message}");
}

// Identity Configuration with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Security requirement: usinque email and same as username
    // Relaxed Password Hash Rules for Development
    options.User.RequireUniqueEmail = true;
    //options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
}).AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();

// Configurar la ruta del Login (Importante para que [Authorize] sepa a dónde ir)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// THIS IS THE KEY: Captures 404, 500, etc.
app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");

// Create a scope to resolve services and run the Seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Call the static method to seed roles and admin user

        await DbInitializer.SeedData(services);
    }
    catch (Exception ex)
    {
        // Log the error if the seeding process fails
        // Registrar el error si el proceso de siembra falla
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseHttpsRedirection();
app.UseRouting();

// Mandatory Security Middleware: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();