using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    // HR-site-specific account controller. Same email/password flow as the public Account controller,
    // but the register form auto-assigns the HR role (no role picker shown).
    [Area("Hr")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ── Register ───────────────────────────
        [HttpGet]
        public IActionResult Register() => View(new HrRegisterFormViewModel());

        [HttpPost]
        public async Task<IActionResult> Register(HrRegisterFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email ini sudah digunakan.");
                return View(model);
            }

            // Stash the registration data for the verify → create-password flow.
            // We use the same shape as PublicRegisterViewModel so the existing AccountController
            // (root) can consume it on POST /Account/CreatePassword.
            var stash = new PublicRegisterViewModel
            {
                FullName = model.FullName,
                Email = model.Email,
                AccountType = "HR"
            };
            TempData["RegisterData"] = JsonSerializer.Serialize(stash);
            return RedirectToAction("VerifyEmail", "Account", new { area = "", email = model.Email });
        }
    }
}
