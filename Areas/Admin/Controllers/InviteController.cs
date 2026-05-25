using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InviteController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _email;
        private readonly ILogger<InviteController> _log;
        private readonly IConfiguration _config;

        public InviteController(UserManager<ApplicationUser> userManager,
                                RoleManager<IdentityRole> roleManager,
                                IEmailSender email,
                                ILogger<InviteController> log,
                                IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _email = email;
            _log = log;
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ActiveNav = "Invite";
            ViewData["Title"] = "Tambah Admin";
            return View(new AdminInviteViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(AdminInviteViewModel model)
        {
            ViewBag.ActiveNav = "Invite";
            ViewData["Title"] = "Tambah Admin";

            if (!ModelState.IsValid) return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email ini sudah terdaftar.");
                return View(model);
            }

            // Ensure Admin role exists (it should from seed, but defensive)
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var newAdmin = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                RoleType = "Admin",
                IsActive = true,
                IsApprovedByAdmin = true
            };
            var result = await _userManager.CreateAsync(newAdmin, model.TempPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }
            await _userManager.AddToRoleAsync(newAdmin, "Admin");

            // Email invitation
            var adminHost = _config["Site:AdminHost"] ?? "admin.lightenup.com";
            try
            {
                await _email.SendAsync(model.Email,
                    "Akun Admin LightenUp telah dibuat",
                    $"Halo {model.FullName},\n\n" +
                    "Akun Admin LightenUp Anda telah dibuat. Silakan login dengan kredensial berikut:\n\n" +
                    $"Email: {model.Email}\n" +
                    $"Kata sandi sementara: {model.TempPassword}\n\n" +
                    $"Login di: https://{adminHost}/AdminAuth/Login\n\n" +
                    "Untuk keamanan, segera ubah kata sandi setelah login pertama di halaman Pengaturan.\n\n" +
                    "— Tim LightenUp");
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Invite email to {Email} failed (SMTP likely not configured)", model.Email);
                TempData["info"] = $"Admin dibuat, tapi email undangan gagal terkirim (SMTP belum dikonfigurasi). Kredensial: {model.Email} / {model.TempPassword}";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = $"Admin {model.FullName} berhasil dibuat dan undangan dikirim via email.";
            return RedirectToAction(nameof(Index));
        }
    }
}
