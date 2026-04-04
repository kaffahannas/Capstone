using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. KONEKSI DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. PENGATURAN IDENTITY (SISTEM AUTENTIKASI)
// Catatan: RequireConfirmedAccount = true berarti login akan DITOLAK 
// jika EmailConfirmed milik user tersebut bernilai 'false' di database.
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // (Opsional) Jika ingin mengatur tingkat kerumitan password, bisa di sini:
    // options.Password.RequireDigit = false;
    // options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3. MVC (Controller & Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. PIPELINE REQUEST HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. MENGAKTIFKAN FITUR LOGIN DAN IZIN AKSES
app.UseAuthentication(); // <-- Sangat Penting! Harus diletakkan SEBELUM Authorization
app.UseAuthorization();

// 6. MENGATUR RUTE URL (Alamat bawaan)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 7. JALANKAN APLIKASI
app.Run();