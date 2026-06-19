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
    // #Class OnboardingController#
    [Authorize(Roles = "HR")]
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;

        public OnboardingController(ApplicationDbContext context,
                                    UserManager<ApplicationUser> userManager,
                                    UserUploadService uploads)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
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

        // #Bagian Welcome#
        // #Function Welcome#
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

        // #Function Welcome POST#

        [HttpPost]
        public async Task<IActionResult> Welcome(bool _)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null || user == null) return RedirectToAction("Login", "Account", new { area = "" });
            return RedirectToAction(NextStepFor(hr, user));
        }

        // #Bagian Foto Profil#
        // #Function Photo#
        [HttpGet]
        public async Task<IActionResult> Photo()
        {
            var hr = await GetHrAsync();
            var user = await _userManager.GetUserAsync(User);
            if (hr == null || user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 1 };
            ViewBag.ExistingPhotoUrl = user.ProfilePicture;
            return View(new HrOnboardingPhotoViewModel { HasExistingPhoto = !string.IsNullOrEmpty(user.ProfilePicture) });
        }

        // #Function Photo POST#

        [HttpPost]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> Photo(HrOnboardingPhotoViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var hasExisting = !string.IsNullOrEmpty(user.ProfilePicture);
            if ((model.Photo == null || model.Photo.Length == 0) && !hasExisting)
                ModelState.AddModelError(nameof(model.Photo), "Silakan unggah foto diri.");

            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new HrOnboardingProgress { Current = 1 };
                ViewBag.ExistingPhotoUrl = user.ProfilePicture;
                model.HasExistingPhoto = hasExisting;
                return View(model);
            }

            if (model.Photo != null && model.Photo.Length > 0)
            {
                var path = await _uploads.ReplaceAsync(
                    user.Id, UserUploadService.Categories.Profile, model.Photo,
                    user.ProfilePicture, allowedExtensions: UserUploadService.ProfileExtensions);
                if (path != null)
                {
                    user.ProfilePicture = path;
                    await _userManager.UpdateAsync(user);
                }
            }

            return RedirectToAction(nameof(Academic));
        }

        // #Bagian Data Akademik#
        // #Function Academic#
        [HttpGet]
        public async Task<IActionResult> Academic()
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 2 };
            ViewBag.ExistingDocumentUrl = hr.AcademicDocumentUrl;
            return View(new HrOnboardingAcademicViewModel
            {
                LastDegree = hr.LastDegree ?? "",
                University = hr.University ?? "",
                HasExistingDocument = !string.IsNullOrEmpty(hr.AcademicDocumentUrl)
            });
        }

        // #Function Academic POST#

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Academic(HrOnboardingAcademicViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            var hasExisting = !string.IsNullOrEmpty(hr.AcademicDocumentUrl);
            if ((model.AcademicDocument == null || model.AcademicDocument.Length == 0) && !hasExisting)
                ModelState.AddModelError(nameof(model.AcademicDocument), "Silakan unggah dokumen pendukung.");

            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new HrOnboardingProgress { Current = 2 };
                ViewBag.ExistingDocumentUrl = hr.AcademicDocumentUrl;
                model.HasExistingDocument = hasExisting;
                return View(model);
            }

            if (model.AcademicDocument != null && model.AcademicDocument.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var path = await _uploads.ReplaceAsync(
                        user.Id, UserUploadService.Categories.Documents, model.AcademicDocument,
                        hr.AcademicDocumentUrl, namePrefix: "academic",
                        allowedExtensions: UserUploadService.DocumentExtensions);
                    if (path != null) hr.AcademicDocumentUrl = path;
                }
            }

            hr.LastDegree = model.LastDegree;
            hr.University = model.University;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Company));
        }

        // #Bagian Data Perusahaan#
        // #Function Company#
        [HttpGet]
        public async Task<IActionResult> Company()
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (hr.OnboardingCompletedAt != null) return RedirectToAction("Index", "Home");

            ViewBag.Progress = new HrOnboardingProgress { Current = 3 };
            ViewBag.ExistingSupportDocumentUrl = hr.SupportDocumentUrl;
            return View(new HrOnboardingCompanyViewModel
            {
                Department = hr.Department ?? "",
                HasExistingSupportDocument = !string.IsNullOrEmpty(hr.SupportDocumentUrl)
            });
        }

        // #Function Company POST#

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Company(HrOnboardingCompanyViewModel model)
        {
            ViewBag.Progress = new HrOnboardingProgress { Current = 3 };

            if (string.IsNullOrWhiteSpace(model.CompanyName))
                ModelState.AddModelError(nameof(model.CompanyName), "Nama perusahaan wajib diisi.");
            if (string.IsNullOrWhiteSpace(model.CompanyAddress))
                ModelState.AddModelError(nameof(model.CompanyAddress), "Lokasi perusahaan wajib diisi.");

            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Login", "Account", new { area = "" });

            ViewBag.ExistingSupportDocumentUrl = hr.SupportDocumentUrl;
            var hasSupport = !string.IsNullOrEmpty(hr.SupportDocumentUrl);

            if (!ModelState.IsValid)
            {
                model.HasExistingSupportDocument = hasSupport;
                return View(model);
            }

            var company = new Company
            {
                Name = model.CompanyName!.Trim(),
                Address = model.CompanyAddress?.Trim(),
                RegistrationNumber = model.RegistrationNumber?.Trim(),
                CreatedAt = DateTime.Now
            };

            if (model.SupportDocument != null && model.SupportDocument.Length > 0)
            {
                var hrUser = await _userManager.GetUserAsync(User);
                if (hrUser != null)
                {
                    var path = await _uploads.ReplaceAsync(
                        hrUser.Id, UserUploadService.Categories.Documents, model.SupportDocument,
                        hr.SupportDocumentUrl, namePrefix: "company",
                        allowedExtensions: UserUploadService.DocumentExtensions);
                    if (path != null) hr.SupportDocumentUrl = path;
                }
            }

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            hr.CompanyId = company.CompanyId;
            hr.Department = model.Department.Trim();
            hr.OnboardingCompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            // HR is now auto-approved; send them straight to subscription purchase.
            return RedirectToAction("Index", "Subscription", new { area = "Hr" });
        }

        // #Bagian Selesai Onboarding#
        // #Function Success#
        [HttpGet]
        public IActionResult Success() => View();
    }
}
