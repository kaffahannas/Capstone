using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<(ApplicationUser? user, HrStaff? hr)> LoadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (null, null);
            var hr = await _context.HrStaffs
                .Include(h => h.Company)
                .Include(h => h.NotificationPreference)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
            return (user, hr);
        }

        // ═════════════════════════════════════════
        //  Index
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (user, hr) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr == null || hr.OnboardingCompletedAt == null) return RedirectToAction("Welcome", "Onboarding");

            var prefs = hr.NotificationPreference ?? new HrNotificationPreference();

            var vm = new HrProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                EmployeeId = hr.EmployeeId,
                Department = hr.Department,
                CompanyName = hr.Company?.Name ?? "",
                ReferralCode = hr.Company?.ReferralCode,
                ProfilePicture = user.ProfilePicture,
                IsActive = user.IsActive,
                RemindEmployeeCheck = prefs.RemindEmployeeCheck,
                RemindCounselingSession = prefs.RemindCounselingSession,
                AllowEmployeePsyNotif = prefs.AllowEmployeePsyNotif,
                Frequency = prefs.Frequency
            };

            ViewBag.ActiveNav = "Profil";
            return View(vm);
        }

        // ═════════════════════════════════════════
        //  Edit
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var (user, hr) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr == null || hr.OnboardingCompletedAt == null) return RedirectToAction("Welcome", "Onboarding");

            var prefs = hr.NotificationPreference ?? new HrNotificationPreference();

            ViewBag.ActiveNav = "Profil";
            return View(new HrProfileEditViewModel
            {
                FullName = user.FullName,
                Phone = user.PhoneNumber,
                EmployeeId = hr.EmployeeId,
                Department = hr.Department,
                CompanyName = hr.Company?.Name ?? "",
                RemindEmployeeCheck = prefs.RemindEmployeeCheck,
                RemindCounselingSession = prefs.RemindCounselingSession,
                AllowEmployeePsyNotif = prefs.AllowEmployeePsyNotif,
                Frequency = prefs.Frequency
            });
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Edit(HrProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Profil";
                return View(model);
            }

            var (user, hr) = await LoadAsync();
            if (user == null || hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (allowed.Contains(ext))
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    Directory.CreateDirectory(folder);
                    var fileName = $"hr_{Guid.NewGuid():N}{ext}";
                    var full = Path.Combine(folder, fileName);
                    using (var s = new FileStream(full, FileMode.Create)) await model.ProfilePicture.CopyToAsync(s);
                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }
            }
            await _userManager.UpdateAsync(user);

            hr.EmployeeId = model.EmployeeId;
            hr.Department = model.Department;

            var prefs = await _context.HrNotificationPreferences.FirstOrDefaultAsync(p => p.HrId == hr.HrId);
            if (prefs == null)
            {
                prefs = new HrNotificationPreference { HrId = hr.HrId };
                _context.HrNotificationPreferences.Add(prefs);
            }
            prefs.RemindEmployeeCheck = model.RemindEmployeeCheck;
            prefs.RemindCounselingSession = model.RemindCounselingSession;
            prefs.AllowEmployeePsyNotif = model.AllowEmployeePsyNotif;
            prefs.Frequency = model.Frequency;

            await _context.SaveChangesAsync();
            TempData["success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }
    }
}
