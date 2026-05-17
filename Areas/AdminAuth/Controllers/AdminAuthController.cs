using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Areas.AdminAuth.Controllers
{
    // Separate login page for the Admin console. Lives only at /AdminAuth/Login.
    // Refuses non-Admin accounts. Refuses login attempts from the customer host.
    [Area("AdminAuth")]
    public class AdminAuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public AdminAuthController(UserManager<ApplicationUser> userManager,
                                   SignInManager<ApplicationUser> signInManager,
                                   IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Block if we're not on the configured admin host (security defense in depth)
            var adminHost = _config["Site:AdminHost"];
            var currentHost = HttpContext.Request.Host.ToString();
            if (!string.IsNullOrEmpty(adminHost) && !currentHost.Equals(adminHost, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, $"Login Admin hanya tersedia di {adminHost}.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email atau kata sandi salah.");
                return View(model);
            }

            // Hard role check BEFORE signing in — never let non-admins through here.
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin") || user.RoleType == "Admin";
            if (!isAdmin)
            {
                ModelState.AddModelError(string.Empty, "Akun ini bukan akun Admin.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Akun Anda dinonaktifkan. Hubungi Admin lain untuk mengaktifkan kembali.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    ModelState.AddModelError(string.Empty, "Akun terkunci karena terlalu banyak percobaan. Coba lagi nanti.");
                else
                    ModelState.AddModelError(string.Empty, "Email atau kata sandi salah.");
                return View(model);
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
