using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace LightenUp.Web.Controllers
{
    [Authorize] // Hanya user yang sudah login yang bisa mengakses
    public class OnboardingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly UserUploadService _uploads;

        public OnboardingController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, UserUploadService uploads)
        {
            _userManager = userManager;
            _context = context;
            _uploads = uploads;
        }

        // ==========================================
        // HALAMAN WELCOME
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Welcome()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.FullName = user.FullName;
            return View();
        }

        // ==========================================
        // LANGKAH 1: FOTO DIRI
        // ==========================================
        [HttpGet]
        public IActionResult Step1()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Step1(OnboardingStep1ViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                if (model.ProfilePhoto != null)
                {
                    var path = await _uploads.ReplaceAsync(
                        user.Id, UserUploadService.Categories.Profile, model.ProfilePhoto,
                        user.ProfilePicture, allowedExtensions: UserUploadService.ProfileExtensions);
                    if (path != null)
                    {
                        user.ProfilePicture = path;
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Lanjut ke langkah 2
                return RedirectToAction("Step2");
            }
            return View(model);
        }

        // ==========================================
        // LANGKAH 2: STATUS AKADEMIK
        // ==========================================
        [HttpGet]
        public IActionResult Step2()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Step2(OnboardingStep2ViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                // Cari data spesifik Psikolog di database berdasarkan UserId
                var psych = _context.Psychologists.FirstOrDefault(p => p.UserId == user.Id);
                if (psych == null) return RedirectToAction("Welcome");

                if (model.AcademicDocument != null)
                {
                    var path = await _uploads.ReplaceAsync(
                        user.Id, UserUploadService.Categories.Documents, model.AcademicDocument,
                        psych.AcademicDocumentUrl, namePrefix: "academic",
                        allowedExtensions: UserUploadService.DocumentExtensions);
                    if (path != null) psych.AcademicDocumentUrl = path;
                }

                // Simpan data teks
                psych.LastDegree = model.LastDegree;
                psych.University = model.University;

                _context.Psychologists.Update(psych);
                await _context.SaveChangesAsync();

                return RedirectToAction("Step3");
            }

            return View(model);
        }

        // ==========================================
        // LANGKAH 3: NOMOR SIPP & STR
        // ==========================================
        [HttpGet]
        public IActionResult Step3()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Step3(OnboardingStep3ViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var psych = _context.Psychologists.FirstOrDefault(p => p.UserId == user.Id);
                if (psych == null) return RedirectToAction("Welcome");

                // Proses Upload Scan STR / SIPP
                if (model.StrDocument != null)
                {
                    var path = await _uploads.ReplaceAsync(
                        user.Id, UserUploadService.Categories.Documents, model.StrDocument,
                        psych.StrDocumentUrl, namePrefix: "str",
                        allowedExtensions: UserUploadService.DocumentExtensions);
                    if (path != null) psych.StrDocumentUrl = path;
                }

                // Simpan data inputan teks
                psych.PracticeLocation = model.PracticeLocation;
                psych.SiapNumber = model.SiapNumber;
                psych.LicenseNumber = model.SippNumber; // Menyimpan SIPP ke kolom LicenseNumber

                _context.Psychologists.Update(psych);
                await _context.SaveChangesAsync();

                // Selesai, arahkan ke halaman berhasil
                return RedirectToAction("Success");
            }

            return View(model);
        }

        // ==========================================
        // HALAMAN SUKSES (AKHIR ONBOARDING)
        // ==========================================
        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}