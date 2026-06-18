using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

// #Bagian Konfigurasi# — lihat docs/konfigurasi.md untuk penjelasan appsettings
// #Bagian Startup Aplikasi#
var builder = WebApplication.CreateBuilder(args);

// #Bagian Konfigurasi Services#
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// #Bagian Identity#
// Identity: ApplicationUser + role support (Patient, Psychologist, HR, Admin)
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// #Bagian Autentikasi Eksternal#
// External login providers (Google, Facebook) — only registered when credentials are configured.
var authBuilder = builder.Services.AddAuthentication();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrWhiteSpace(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });
}

// #Bagian Registrasi Service Aplikasi#
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpClient();
builder.Services.Configure<LightenUp.Web.Services.DuitkuOptions>(
    builder.Configuration.GetSection(LightenUp.Web.Services.DuitkuOptions.SectionName));
builder.Services.AddScoped<LightenUp.Web.Services.HealthStatusService>();
builder.Services.AddScoped<LightenUp.Web.Services.UserUploadService>();
builder.Services.AddScoped<LightenUp.Web.Services.DuitkuService>();
builder.Services.AddScoped<LightenUp.Web.Services.SubscriptionAccessService>();
builder.Services.AddScoped<LightenUp.Web.Services.SubscriptionPricingService>();
builder.Services.AddScoped<LightenUp.Web.Services.PsychologistWorkloadService>();
builder.Services.AddScoped<LightenUp.Web.Services.AssignmentActivationService>();
builder.Services.AddScoped<LightenUp.Web.Services.IEmailSender, LightenUp.Web.Services.SmtpEmailSender>();

// #Bagian Rate Limiting#
// Rate limiting for login/register endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// #Bagian Health Check#
// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// #Bagian Database Seeding#
// ========================================================
// DATABASE SEEDING (runs once at startup, in any environment)
// ========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create/update schema before any Identity queries (roles/admin need existing tables).
        await context.Database.MigrateAsync();

        // 1. Ensure Identity roles exist
        string[] roles = { "Admin", "Patient", "Psychologist", "HR" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Ensure default admin exists
        // Admin credentials are read from configuration (appsettings / User Secrets / env vars).
        // NEVER hardcode credentials in source code.
        var adminEmail = builder.Configuration["Seed:AdminEmail"] ?? "admin@lightenup.com";
        var adminPassword = builder.Configuration["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            var seedLogger = services.GetRequiredService<ILogger<Program>>();
            seedLogger.LogWarning(
                "Seed:AdminPassword not configured. Set it via User Secrets or environment variables. " +
                "Skipping admin seed.");
        }
        else
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Admin",
                    RoleType = "Admin",
                    IsApprovedByAdmin = true
                };

                var result = await userManager.CreateAsync(newAdmin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }

        // 3. Seed domain data (Companies, Psychologists, Patients, Schedules, ...)
        // if (app.Environment.IsDevelopment())
        // {
        //     await DbInitializer.SeedAsync(context, userManager);
        // }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// #Bagian Pipeline HTTP#
// --- HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

// #Bagian Hostname Area Gating#
// ───── Hostname-based area gating ─────
// Customer site (Site:PatientHost) hosts Patient/Psychologist/HR. /Admin/* is BLOCKED here.
// Admin console (Site:AdminHost) hosts only LightenUp staff. Only /Admin*, /AdminAuth*, static reachable.
var patientHost = builder.Configuration["Site:PatientHost"];
var adminHost = builder.Configuration["Site:AdminHost"];
if (!string.IsNullOrWhiteSpace(patientHost) || !string.IsNullOrWhiteSpace(adminHost))
{
    app.Use(async (context, next) =>
    {
        var host = context.Request.Host.ToString();
        var path = context.Request.Path.Value ?? string.Empty;
        var isAdminPath = path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith("/AdminAuth", StringComparison.OrdinalIgnoreCase);

        // On the customer host, block anything under /Admin or /AdminAuth.
        if (!string.IsNullOrWhiteSpace(patientHost) &&
            host.Equals(patientHost, StringComparison.OrdinalIgnoreCase) && isAdminPath)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // On the Admin host, only allow Admin paths + static files.
        if (!string.IsNullOrWhiteSpace(adminHost) &&
            host.Equals(adminHost, StringComparison.OrdinalIgnoreCase) && !isAdminPath)
        {
            var allowed =
                path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);

            if (!allowed)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            if (path.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/AdminAuth/Login");
                return;
            }
        }

        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// #Bagian Routing#
// Short admin login URL (avoids /AdminAuth/AdminAuth/Login from default area routing).
app.MapControllerRoute(
    name: "admin_auth_login",
    pattern: "AdminAuth/Login",
    defaults: new { area = "AdminAuth", controller = "AdminAuth", action = "Login" });

// Area routes come first so /Patient/* and /Hr/* are matched before the default.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
