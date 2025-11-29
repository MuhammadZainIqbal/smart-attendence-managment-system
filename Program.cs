using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Required for IEmailSender
using Microsoft.EntityFrameworkCore;
using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Services; // Required for EmailSender
using Microsoft.Extensions.Configuration; // Required for IConfiguration

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// --- Identity Service Registration (Unified and Correct) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
// -----------------------------------------------------------

// 💥 CRITICAL FIX: Register the REAL EmailSender service as a Singleton
// This uses Dependency Injection to provide IEmailSender whenever it's requested (e.g., by RegisterModel)
builder.Services.AddSingleton<IEmailSender, EmailSender>();

// Add services for MVC Controllers
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AttendenceManagementSystem.Filters.EnsurePasswordChangedAttribute>();
});

// Registers the services required to run Razor Pages (used by the Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();

// --- Role Seeding: Initialize Admin, Teacher, Student roles ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Seed roles on application startup
    await AttendenceManagementSystem.Services.RoleSeeder.SeedRolesAsync(roleManager);
}
// -------------------------------------------------------------

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();

app.UseAuthorization();

// Maps the routes for all Razor Pages, including the Identity UI pages
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();