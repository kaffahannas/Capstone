using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity: ApplicationUser + role support (Patient, Psychologist, HR, Admin)
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<LightenUp.Web.Services.HealthStatusService>();
builder.Services.AddScoped<LightenUp.Web.Services.IEmailSender, LightenUp.Web.Services.SmtpEmailSender>();

var app = builder.Build();

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

        // 1. Ensure Identity roles exist
        string[] roles = { "Admin", "Patient", "Psychologist", "HR" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Ensure default admin exists
        const string adminEmail = "admin@lightenup.com";
        const string adminPassword = "Admin123!";
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
                IsApprovedByHR = true
            };

            var result = await userManager.CreateAsync(newAdmin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
            }
        }

        // 3. Seed domain data (Companies, Psychologists, Patients, Schedules, ...)
        await DbInitializer.SeedAsync(context, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

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

// ───── Hostname-based area gating ─────
// In production, the Patient host (e.g. app.lightenup.com) cannot reach /Hr/*,
// and the HR host (e.g. hr.lightenup.com) cannot reach Patient/Psychologist routes.
// In dev, set Site:PatientHost=localhost:7040 and Site:HrHost=localhost:7041
// in user-secrets or appsettings.Development.json.
var patientHost = builder.Configuration["Site:PatientHost"];
var hrHost = builder.Configuration["Site:HrHost"];
if (!string.IsNullOrWhiteSpace(patientHost) || !string.IsNullOrWhiteSpace(hrHost))
{
    app.Use(async (context, next) =>
    {
        var host = context.Request.Host.ToString();
        var path = context.Request.Path.Value ?? string.Empty;
        var isHrPath = path.StartsWith("/Hr", StringComparison.OrdinalIgnoreCase);

        // On the patient host, block anything under /Hr.
        if (!string.IsNullOrWhiteSpace(patientHost) &&
            host.Equals(patientHost, StringComparison.OrdinalIgnoreCase) && isHrPath)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // On the HR host, only allow /Hr/*, the shared login/account routes, and static files.
        if (!string.IsNullOrWhiteSpace(hrHost) &&
            host.Equals(hrHost, StringComparison.OrdinalIgnoreCase) && !isHrPath)
        {
            var allowed =
                path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/Identity/", StringComparison.OrdinalIgnoreCase) ||
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
        }

        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Area routes come first so /Patient/* and /Hr/* are matched before the default.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
