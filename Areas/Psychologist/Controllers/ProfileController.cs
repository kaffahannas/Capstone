using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    // #Class ProfileController#
    [Authorize(Roles = "Psychologist")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserUploadService uploads)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
        }

        // #Function PayrollAgreement#

        [HttpGet]
        public async Task<IActionResult> PayrollAgreement()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var psychologist = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psychologist == null) return NotFound("Data Psikolog tidak ditemukan.");

            var payrollSetting = await _context.PayrollSettings.FirstOrDefaultAsync(ps => ps.PsychologistId == psychologist.PsychologistId);
            if (payrollSetting != null && payrollSetting.AgreementStatus != "None")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // #Function SubmitPayrollAgreement#

        [HttpPost]
        public async Task<IActionResult> SubmitPayrollAgreement()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var psychologist = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psychologist == null) return NotFound("Data Psikolog tidak ditemukan.");

            var payrollSetting = await _context.PayrollSettings.FirstOrDefaultAsync(ps => ps.PsychologistId == psychologist.PsychologistId);
            if (payrollSetting == null)
            {
                payrollSetting = new PsychologistPayrollSetting
                {
                    PsychologistId = psychologist.PsychologistId,
                    PsychologistPercentage = SubscriptionPricingService.DefaultPsychologistRevenuePercentage,
                    AgreementStatus = "None"
                };
                _context.PayrollSettings.Add(payrollSetting);
            }

            payrollSetting.AgreementStatus = "Approved";
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Persetujuan berhasil disimpan. Selamat datang di Dashboard!";

            return RedirectToAction("Index", "Dashboard");
        }

        // #Function Profile#

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var psych = await _context.Psychologists

                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            var activeCases = await _context.Assignments
                .CountAsync(a => a.PsychologistId == psych.PsychologistId && (a.Status == "Active" || a.Status == "PendingCancellation"));
            var employeesCount = await _context.Assignments
                .Where(a => a.PsychologistId == psych.PsychologistId)
                .Select(a => a.PatientId).Distinct().CountAsync();


            var payrollSetting = await _context.PayrollSettings.FirstOrDefaultAsync(ps => ps.PsychologistId == psych.PsychologistId);

            var viewModel = new LightenUp.Web.Models.ViewModels.PsyProfileExtViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,
                Specialization = psych.Specialization,
                Bio = psych.Bio,
                LastDegree = psych.LastDegree,
                University = psych.University,
                PracticeLocation = psych.PracticeLocation,
                OfficeAddress = psych.OfficeAddress,
                SiapNumber = psych.SiapNumber,
                SippNumber = psych.LicenseNumber,
                ExperienceYears = psych.ExperienceYears,
                IsActive = user.IsActive,
                AvailabilityText = string.IsNullOrEmpty(psych.AvailabilityText) ? "Mon-Fri: 9AM-5PM" : psych.AvailabilityText!,
                IsAvailable = psych.IsAvailable,
                Employees = employeesCount,
                ActiveCases = activeCases,
                AcceptsB2B = psych.AcceptsB2B,

                BankDetailsPdfPath = payrollSetting?.BankDetailsPdfPath
            };

            return View(viewModel);
        }

        // #Function EditProfile#

        [HttpPost]
        public async Task<IActionResult> EditProfile(LightenUp.Web.Models.ViewModels.EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            user.FullName    = model.FullName.Trim();
            user.PhoneNumber = model.Phone?.Trim();

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                var newPath = await _uploads.ReplaceAsync(
                    user.Id,
                    UserUploadService.Categories.Profile,
                    model.ProfilePictureFile,
                    user.ProfilePicture,
                    allowedExtensions: UserUploadService.ProfileExtensions);
                if (newPath != null)
                    user.ProfilePicture = newPath;
            }

            if (model.BankDocumentFile != null && model.BankDocumentFile.Length > 0)
            {
                var payrollSetting = await _context.PayrollSettings.FirstOrDefaultAsync(ps => ps.PsychologistId == psych.PsychologistId);
                if (payrollSetting != null)
                {
                    var newPath = await _uploads.ReplaceAsync(
                        user.Id,
                        "payroll_agreements",
                        model.BankDocumentFile,
                        payrollSetting.BankDetailsPdfPath,
                        allowedExtensions: new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" });
                    
                    if (newPath != null)
                        payrollSetting.BankDetailsPdfPath = newPath;
                }
            }

            psych.Specialization   = model.Specialization?.Trim();
            psych.Bio              = model.Bio?.Trim();
            psych.LastDegree       = model.LastDegree?.Trim();
            psych.University       = model.University?.Trim();
            psych.PracticeLocation = model.PracticeLocation?.Trim();
            psych.OfficeAddress    = model.OfficeAddress?.Trim();
            psych.LicenseNumber    = model.SippNumber?.Trim();
            psych.ExperienceYears  = model.ExperienceYears;

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Profile));
        }

        // #Function SetAvailability#

        [HttpPost]
        public async Task<IActionResult> SetAvailability(bool isAvailable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych != null)
            {
                psych.IsAvailable = isAvailable;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Profile));
        }



        // #Function Settings#



        [HttpGet]
        public IActionResult Settings()
        {
            return RedirectToAction(nameof(Profile));
        }

        // #Function Settings POST#

        [HttpPost]
        public async Task<IActionResult> Settings(LightenUp.Web.Models.ViewModels.PsySettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            psy.AcceptsB2B = model.AcceptsB2B;
            await _context.SaveChangesAsync();
            TempData["success"] = "Pengaturan B2B disimpan.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
