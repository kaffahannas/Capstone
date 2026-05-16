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
    // 3-step HR onboarding wizard.
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public OnboardingController(ApplicationDbContext context,
                                    UserManager<ApplicationUser> userManager,
                                    IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var hr = await _context.HrStaffs.FirstOrDefaultAsync(h => h.UserId == user.Id);
            if (hr == null)
            {
                hr = new HrStaff { UserId = user.Id };
                _context.HrStaffs.Add(hr);
                await _context.SaveChangesAsync();
            }
            return hr;
        }

        private static string NextStepFor(HrStaff hr, ApplicationUser user)
        {
            if (string.IsNullOrEmpty(user.ProfilePicture)) return nameof(Photo);
            if (string.IsNullOrEmpty(hr.LastDegree) || string.IsNullOrEmpty(hr.University)) return nameof(Academic);
            if (hr.CompanyId == null) return nameof(Company);
            return nameof(Success);
        }

        // ═════════════════════════════════════════════
        //  Welcome
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Welcome()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.HrName = user.FullName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Welcome(bool _)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null || user == null) return RedirectToAction("Login", "Account", new { area = "" });
            return RedirectToAction(NextStepFor(hr, user));
        }

        // ═════════════════════════════════════════════
        //  Step 1 — Photo
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Photo()
        {
            var hr = await GetHrAsync();
            var user = await _userManager.GetUserAsync(User);
            if (hr == null || user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 1 };
            return View(new HrOnboardingPhotoViewModel());
        }

        [HttpPost]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> Photo(HrOnboardingPhotoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new HrOnboardingProgress { Current = 1 };
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (model.Photo != null && model.Photo.Length > 0)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(folder);
                var fileName = $"hr_{Guid.NewGuid():N}{ext}";
                var full = Path.Combine(folder, fileName);
                using (var s = new FileStream(full, FileMode.Create)) await model.Photo.CopyToAsync(s);
                user.ProfilePicture = $"/uploads/profiles/{fileName}";
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction(nameof(Academic));
        }

        // ═════════════════════════════════════════════
        //  Step 2 — Academic
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Academic()
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 2 };
            return View(new HrOnboardingAcademicViewModel
            {
                LastDegree = hr.LastDegree ?? "",
                University = hr.University ?? ""
            });
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Academic(HrOnboardingAcademicViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new HrOnboardingProgress { Current = 2 };
                return View(model);
            }

            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (model.AcademicDocument != null && model.AcademicDocument.Length > 0)
            {
                var ext = Path.GetExtension(model.AcademicDocument.FileName).ToLowerInvariant();
                var folder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                Directory.CreateDirectory(folder);
                var fileName = $"hr_academic_{Guid.NewGuid():N}{ext}";
                var full = Path.Combine(folder, fileName);
                using (var s = new FileStream(full, FileMode.Create)) await model.AcademicDocument.CopyToAsync(s);
                hr.AcademicDocumentUrl = $"/uploads/documents/{fileName}";
            }

            hr.LastDegree = model.LastDegree;
            hr.University = model.University;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Company));
        }

        // ═════════════════════════════════════════════
        //  Step 3 — Company (create new OR join existing)
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Company()
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 3 };
            return View(new HrOnboardingCompanyViewModel
            {
                Mode = "Create",
                Department = hr.Department ?? ""
            });
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Company(HrOnboardingCompanyViewModel model)
        {
            ViewBag.Progress = new HrOnboardingProgress { Current = 3 };

            // Per-mode validation
            if (model.Mode == "Create")
            {
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                    ModelState.AddModelError(nameof(model.CompanyName), "Nama perusahaan wajib diisi.");
                if (string.IsNullOrWhiteSpace(model.CompanyAddress))
                    ModelState.AddModelError(nameof(model.CompanyAddress), "Lokasi perusahaan wajib diisi.");
            }
            else if (model.Mode == "Join")
            {
                if (string.IsNullOrWhiteSpace(model.ReferralCode))
                    ModelState.AddModelError(nameof(model.ReferralCode), "Kode referral wajib diisi.");
            }
            else
            {
                ModelState.AddModelError(nameof(model.Mode), "Pilih opsi yang valid.");
            }

            if (!ModelState.IsValid) return View(model);

            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (model.Mode == "Create")
            {
                // Generate a referral code (6 uppercase alphanumeric chars), retry if collision.
                string code;
                do { code = GenerateReferralCode(); }
                while (await _context.Companies.AnyAsync(c => c.ReferralCode == code));

                var company = new Company
                {
                    Name = model.CompanyName!.Trim(),
                    Address = model.CompanyAddress?.Trim(),
                    RegistrationNumber = model.RegistrationNumber?.Trim(),
                    ReferralCode = code,
                    CreatedAt = DateTime.Now
                };

                if (model.SupportDocument != null && model.SupportDocument.Length > 0)
                {
                    var ext = Path.GetExtension(model.SupportDocument.FileName).ToLowerInvariant();
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                    Directory.CreateDirectory(folder);
                    var fileName = $"hr_company_{Guid.NewGuid():N}{ext}";
                    var full = Path.Combine(folder, fileName);
                    using (var s = new FileStream(full, FileMode.Create)) await model.SupportDocument.CopyToAsync(s);
                    hr.SupportDocumentUrl = $"/uploads/documents/{fileName}";
                }

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                hr.CompanyId = company.CompanyId;
            }
            else // Join
            {
                var target = await _context.Companies
                    .FirstOrDefaultAsync(c => c.ReferralCode == model.ReferralCode!.Trim());
                if (target == null)
                {
                    ModelState.AddModelError(nameof(model.ReferralCode), "Kode referral tidak ditemukan.");
                    return View(model);
                }
                hr.CompanyId = target.CompanyId;
            }

            hr.Department = model.Department.Trim();
            hr.OnboardingCompletedAt = DateTime.UtcNow;

            // Create default notification preferences
            _context.HrNotificationPreferences.Add(new HrNotificationPreference { HrId = hr.HrId });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Success));
        }

        // ═════════════════════════════════════════════
        //  Success
        // ═════════════════════════════════════════════
        [HttpGet]
        public IActionResult Success() => View();

        // Helper — 6-char alphanumeric uppercase (avoid 0/O/I/1 ambiguity)
        private static string GenerateReferralCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = Random.Shared;
            return new string(Enumerable.Range(0, 6).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
        }
    }
}
