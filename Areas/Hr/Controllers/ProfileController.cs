using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    // #Class ProfileController#
    [Authorize(Roles = "HR")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;
        private readonly SubscriptionAccessService _access;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            UserUploadService uploads, SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
            _access = access;
        }

        private async Task<(ApplicationUser? user, HrStaff? hr)> LoadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (null, null);
            var hr = await _context.HrStaffs
                .Include(h => h.Company)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
            return (user, hr);
        }

        // ═════════════════════════════════════════
        //  Index
        // ═════════════════════════════════════════
        // #Function Index#
                [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (user, hr) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr == null || hr.OnboardingCompletedAt == null) return RedirectToAction("Welcome", "Onboarding");

            CompanySubscription? activeSub = null;
            int divisionCount     = 0;
            int activeEmployeeCount = 0;

            if (hr.CompanyId != null)
            {
                activeSub           = await _access.GetActiveCompanySubscriptionAsync(hr.CompanyId.Value);
                divisionCount       = await _context.CompanyDivisions.CountAsync(d => d.CompanyId == hr.CompanyId);
                activeEmployeeCount = await _context.Patients.CountAsync(
                    p => p.CompanyId == hr.CompanyId && p.EmploymentStatus == "active");
            }

            var vm = new HrProfileViewModel
            {
                // Personal
                FullName            = user.FullName,
                Email               = user.Email ?? "",
                Phone               = user.PhoneNumber,
                ProfilePicture      = user.ProfilePicture,
                IsActive            = user.IsActive,
                IsApprovedByAdmin   = user.IsApprovedByAdmin,
                OnboardingCompletedAt = hr.OnboardingCompletedAt,

                // Employment
                EmployeeId  = hr.EmployeeId,
                Department  = hr.Department,

                // Company
                CompanyName               = hr.Company?.Name ?? "",
                CompanyAddress            = hr.Company?.Address,
                CompanyRegistrationNumber = hr.Company?.RegistrationNumber,
                CompanyReferralCode       = hr.Company?.ReferralCode,
                CompanyContactEmail       = hr.Company?.ContactEmail,
                CompanyContactNumber      = hr.Company?.ContactNumber,
                DivisionCount             = divisionCount,
                ActiveEmployeeCount       = activeEmployeeCount,

                // Subscription
                HasActiveSubscription = activeSub != null,
                ActivePlanName        = activeSub?.PlanName,
                ActiveUntil           = activeSub?.EndDate,
            };

            ViewBag.ActiveNav = "Profil";
            return View(vm);
        }

        // ═════════════════════════════════════════
        //  Edit  (fallback full-page, kept for safety)
        // ═════════════════════════════════════════
        // #Function Edit#
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var (user, hr) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr == null || hr.OnboardingCompletedAt == null) return RedirectToAction("Welcome", "Onboarding");

            ViewBag.ActiveNav = "Profil";
            return View(new HrProfileEditViewModel
            {
                FullName    = user.FullName,
                Phone       = user.PhoneNumber,
                EmployeeId  = hr.EmployeeId,
                Department  = hr.Department,
                CompanyName = hr.Company?.Name ?? "",
            });
        }

        // #Function Edit POST#

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

            user.FullName    = model.FullName;
            user.PhoneNumber = model.Phone;

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var path = await _uploads.ReplaceAsync(
                    user.Id, UserUploadService.Categories.Profile, model.ProfilePicture,
                    user.ProfilePicture, allowedExtensions: UserUploadService.ProfileExtensions);
                if (path != null) user.ProfilePicture = path;
            }
            await _userManager.UpdateAsync(user);

            hr.EmployeeId = model.EmployeeId;
            hr.Department = model.Department;
            await _context.SaveChangesAsync();

            TempData["success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }

        // ═════════════════════════════════════════
        //  EditModal  (AJAX — used by profile modal)
        // ═════════════════════════════════════════
        // #Function EditModal#
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> EditModal(HrProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { ok = false, errors });
            }

            var (user, hr) = await LoadAsync();
            if (user == null || hr == null)
                return Json(new { ok = false, errors = new[] { "Sesi tidak valid. Silakan login ulang." } });

            user.FullName    = model.FullName;
            user.PhoneNumber = model.Phone;

            bool pictureChanged = false;
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var path = await _uploads.ReplaceAsync(
                    user.Id, UserUploadService.Categories.Profile, model.ProfilePicture,
                    user.ProfilePicture, allowedExtensions: UserUploadService.ProfileExtensions);
                if (path != null) { user.ProfilePicture = path; pictureChanged = true; }
            }
            await _userManager.UpdateAsync(user);

            hr.EmployeeId = model.EmployeeId;
            hr.Department = model.Department;
            await _context.SaveChangesAsync();

            return Json(new
            {
                ok             = true,
                fullName       = user.FullName,
                phone          = user.PhoneNumber,
                department     = hr.Department,
                employeeId     = hr.EmployeeId,
                pictureChanged,
            });
        }

        // ═════════════════════════════════════════
        //  ChangePassword  (AJAX modal)
        // ═════════════════════════════════════════
        // #Function ChangePassword#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            [FromForm] string? currentPassword,
            [FromForm] string? newPassword,
            [FromForm] string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
                return Json(new { ok = false, errors = new[] { "Password saat ini wajib diisi." } });
            if (string.IsNullOrWhiteSpace(newPassword))
                return Json(new { ok = false, errors = new[] { "Password baru wajib diisi." } });
            if (newPassword.Length < 6)
                return Json(new { ok = false, errors = new[] { "Password baru minimal 6 karakter." } });
            if (newPassword != confirmPassword)
                return Json(new { ok = false, errors = new[] { "Konfirmasi password tidak cocok dengan password baru." } });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { ok = false, errors = new[] { "Sesi tidak valid. Silakan login ulang." } });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errs = result.Errors.Select(e => e.Description).ToArray();
                return Json(new { ok = false, errors = errs });
            }

            return Json(new { ok = true, message = "Password berhasil diubah." });
        }
    }
}
