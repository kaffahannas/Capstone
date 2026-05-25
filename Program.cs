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
builder.Services.AddHttpClient();
builder.Services.Configure<LightenUp.Web.Services.DuitkuOptions>(
    builder.Configuration.GetSection(LightenUp.Web.Services.DuitkuOptions.SectionName));
builder.Services.AddScoped<LightenUp.Web.Services.HealthStatusService>();
builder.Services.AddScoped<LightenUp.Web.Services.UserUploadService>();
builder.Services.AddScoped<LightenUp.Web.Services.DuitkuService>();
builder.Services.AddScoped<LightenUp.Web.Services.SubscriptionAccessService>();
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
                IsApprovedByAdmin = true
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

app.Run();
