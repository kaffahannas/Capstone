using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    // #Class SettingsController#
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SettingsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // #Function Index#

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ViewBag.ActiveNav = "Settings";
            ViewData["Title"] = "Pengaturan Akun Saya";
            return View(new AdminSettingsViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber
            });
        }

        // #Function UpdateProfile#

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(AdminSettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;
            await _userManager.UpdateAsync(user);
            TempData["success"] = "Profil diperbarui.";
            return RedirectToAction(nameof(Index));
        }

        // #Function ChangePassword#

        [HttpPost]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel model)
        {
            ViewBag.ActiveNav = "Settings";
            ViewData["Title"] = "Pengaturan Akun Saya";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                TempData["error"] = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["success"] = "Kata sandi berhasil diubah.";
            return RedirectToAction(nameof(Index));
        }
    }
}
