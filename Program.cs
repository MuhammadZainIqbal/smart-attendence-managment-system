using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Required for IEmailSender
using Microsoft.EntityFrameworkCore;
using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Services; // Required for EmailSender
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// --- Identity Service Registration (Unified and Correct) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
// -----------------------------------------------------------

// --- Cookie Configuration for Session Management ---
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie Security Settings
    options.Cookie.HttpOnly = true;                      // Prevent XSS access to cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS in prod, HTTP in dev
    options.Cookie.SameSite = SameSiteMode.Lax;          // Allow redirects from external sites
    options.Cookie.IsEssential = true;                   // Required for GDPR cookie consent
    options.Cookie.Name = ".AspNetCore.Identity.Application"; // Explicit cookie name
    
    // Session Timeout Settings
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);   // Cookie validity duration
    options.SlidingExpiration = true;                    // Reset expiry on each request
    
    // CRITICAL: IsPersistent is controlled by RememberMe checkbox at login time
    // When RememberMe = false, browser closes = cookie deleted (session cookie)
    // When RememberMe = true, cookie persists for ExpireTimeSpan
    
    // Redirect Paths
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    
    // Events for custom handling
    options.Events.OnSigningIn = context =>
    {
        // Log sign-in events for debugging
        var isPersistent = context.Properties?.IsPersistent ?? false;
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CookieAuth");
        logger.LogInformation("User signing in. IsPersistent (RememberMe): {IsPersistent}", isPersistent);
        return Task.CompletedTask;
    };
});
// ---------------------------------------------------

// --- JWT Bearer Authentication Configuration ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(options =>
{
    // Default scheme remains Cookie for MVC views (backward compatible)
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // No tolerance for token expiry
    };
    
    // Allow JWT token from cookie named "JWToken" (for MVC compatibility)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // DEBUG: Log all cookies present in the request
            var allCookies = string.Join(", ", context.Request.Cookies.Keys);
            Console.WriteLine($"🔍 JWT OnMessageReceived - All cookies: [{allCookies}]");
            
            // Check for JWT in cookie (for MVC views)
            if (context.Request.Cookies.ContainsKey("JWToken"))
            {
                context.Token = context.Request.Cookies["JWToken"];
                Console.WriteLine($"✅ JWT FOUND in 'JWToken' cookie! Token length: {context.Token?.Length ?? 0}");
            }
            else
            {
                Console.WriteLine("❌ 'JWToken' cookie NOT found in request.");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});
// ------------------------------------------------

// 💥 CRITICAL FIX: Register the REAL EmailSender service using HttpClientFactory
// This uses Dependency Injection to provide IEmailSender whenever it's requested.
// Using AddHttpClient ensures efficient connection pooling for the raw REST API calls.
builder.Services.AddHttpClient<IEmailSender, EmailSender>();

// --- Register TokenService for JWT generation ---
builder.Services.AddScoped<ITokenService, TokenService>();
// ------------------------------------------------

// Add services for MVC Controllers
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AttendenceManagementSystem.Filters.EnsurePasswordChangedAttribute>();
});

// Registers the services required to run Razor Pages (used by the Identity UI)
builder.Services.AddRazorPages();

var app = builder.Build();



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

// --- Automated Database Migration on Startup ---
// Creates a clean scope for cold start migration execution in Azure Linux containers
using (var migrationScope = app.Services.CreateScope())
{
    var scopedServices = migrationScope.ServiceProvider;
    try
    {
        var context = scopedServices.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // --- Role Seeding: Initialize Admin, Teacher, Student roles ---
        // Seeding roles must happen AFTER successful schema migration
        var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();
        await AttendenceManagementSystem.Services.RoleSeeder.SeedRolesAsync(roleManager);
        // -------------------------------------------------------------
    }
    catch (Exception ex)
    {
        var logger = scopedServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database during container cold start.");
        throw; // Let the container crash gracefully to avoid silent database desync
    }
}
// -----------------------------------------------

app.Run();