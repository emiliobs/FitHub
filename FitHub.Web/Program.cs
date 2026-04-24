using FitHub.Web.Data;
using FitHub.Web.Models.Identity;
using FitHub.Web.Options;
using FitHub.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Configure SQL Server Connection
try
{
    // Database configuration is critical. Ensure the connection string is correct and the database server is accessible.
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        // Use SQL Server with the connection string from appsettings.json
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

// Authorization policies based on roles (Admin, Manager) for class and billing management
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageClasses", policy =>
        policy.RequireRole("Admin", "Manager"));

    options.AddPolicy("CanManageBilling", policy =>
        policy.RequireRole("Admin", "Manager"));
});

// Configurar la ruta del Login (Importante para que [Authorize] sepa a dónde ir)
builder.Services.ConfigureApplicationCookie(options =>
{
    // This is crucial for handling unauthorized access. It ensures that users are redirected
    // to the login page when they try to access protected resources.
    options.LoginPath = "/Account/Login";

    // This is important for handling access denied scenarios. It ensures that users
    // are redirected to a specific page when they try to access resources they don't have permission for.
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// configuration injection depesdenc service
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInstructorSyncService, InstructorSyncService>();
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

// This is mandatory to serve images from wwwroot
app.UseStaticFiles();

// Custom extension method to serve static assets from the "Assets" folder
app.MapStaticAssets();

//  Default route configuration for MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Run the application
app.Run();