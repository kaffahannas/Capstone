using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HealthStatusService _healthService;
        private readonly IWebHostEnvironment _env;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            HealthStatusService healthService,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _healthService = healthService;
            _env = env;
        }

        private async Task<(ApplicationUser? user, LightenUp.Web.Models.Patient? patient)> LoadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return (null, null);
            var patient = await _context.Patients
                .Include(p => p.Company)
                .Include(p => p.NotificationPreference)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            return (user, patient);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Index — profile page
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var (user, patient) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient == null) return RedirectToAction("Welcome", "Onboarding");

            // Refresh computed status before showing.
            await _healthService.UpdateAndSaveAsync(patient);
            var snap = await _healthService.ComputeAsync(patient.PatientId);

            // Sessions
            var now = DateTime.Now;
            var lastSession = await _context.Schedules
                .Where(s => s.PatientId == patient.PatientId && s.Status == "Completed")
                .OrderByDescending(s => s.SessionStart)
                .Select(s => (DateTime?)s.SessionStart)
                .FirstOrDefaultAsync();
            var nextSession = await _context.Schedules
                .Where(s => s.PatientId == patient.PatientId && s.Status == "Scheduled" && s.SessionStart > now)
                .OrderBy(s => s.SessionStart)
                .Select(s => (DateTime?)s.SessionStart)
                .FirstOrDefaultAsync();

            // Assigned psychologist (for Kontak Psychologist mailto)
            var psyEmail = await _context.Assignments
                .Where(a => a.PatientId == patient.PatientId && a.Status == "Active")
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => a.Psychologist!.User!.Email)
                .FirstOrDefaultAsync();

            var prefs = patient.NotificationPreference ?? new PatientNotificationPreference
            {
                PatientId = patient.PatientId
            };

            var vm = new PatientProfileViewModel
            {
                FullName = user.FullName,
                ProfilePicture = user.ProfilePicture,
                MentalHealthStatus = patient.MentalHealthStatus,

                IsB2B = patient.CompanyId != null,
                Department = patient.Department,
                EmployeeId = patient.EmployeeId,
                CompanyName = patient.Company?.Name,

                Email = user.Email ?? "",
                Phone = user.PhoneNumber,

                TotalMoodPercent = snap.TotalMoodPercent,
                LastCheckLabel = snap.LastCheckLabel,

                LastSessionAt = lastSession,
                NextSessionAt = nextSession,

                RemindMoodCheck = prefs.RemindMoodCheck,
                RemindCounselingSession = prefs.RemindCounselingSession,
                AllowHrPsychologistNotif = prefs.AllowHrPsychologistNotif,
                ReminderTime = prefs.ReminderTime.ToString(@"hh\:mm"),

                EmergencyContactEmail = patient.EmergencyContactEmail,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                CompanyHrEmail = patient.Company?.ContactEmail,
                PsychologistEmail = psyEmail
            };

            ViewBag.ActiveNav = "Profil";
            return View(vm);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Edit — full edit form
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var (user, patient) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient == null) return RedirectToAction("Welcome", "Onboarding");

            var prefs = patient.NotificationPreference ?? new PatientNotificationPreference { PatientId = patient.PatientId };

            var vm = new PatientProfileEditViewModel
            {
                FullName = user.FullName,
                Phone = user.PhoneNumber,
                Department = patient.Department,
                EmployeeId = patient.EmployeeId,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactEmail = patient.EmergencyContactEmail,
                EmergencyContactRelation = patient.EmergencyContactRelation,
                RemindMoodCheck = prefs.RemindMoodCheck,
                RemindCounselingSession = prefs.RemindCounselingSession,
                AllowHrPsychologistNotif = prefs.AllowHrPsychologistNotif,
                ReminderTime = prefs.ReminderTime,
                IsAlreadyB2B = patient.CompanyId != null,
                CurrentCompanyName = patient.Company?.Name
            };

            ViewBag.ActiveNav = "Profil";
            return View(vm);
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Edit(PatientProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Profil";
                return View(model);
            }

            var (user, patient) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient == null) return RedirectToAction("Welcome", "Onboarding");

            // ApplicationUser fields
            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;

            // Profile photo upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (allowed.Contains(ext))
                {
                    var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    Directory.CreateDirectory(folder);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var full = Path.Combine(folder, fileName);
                    using (var s = new FileStream(full, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(s);
                    }
                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }
            }
            await _userManager.UpdateAsync(user);

            // Patient fields
            patient.Department = model.Department;
            patient.EmployeeId = model.EmployeeId;
            patient.DateOfBirth = model.DateOfBirth;
            patient.Gender = model.Gender;
            patient.EmergencyContactName = model.EmergencyContactName;
            patient.EmergencyContactPhone = model.EmergencyContactPhone;
            patient.EmergencyContactEmail = model.EmergencyContactEmail;
            patient.EmergencyContactRelation = model.EmergencyContactRelation;

            // Referral code → assign company if valid and patient isn't already B2B
            if (!string.IsNullOrWhiteSpace(model.ReferralCode) && patient.CompanyId == null)
            {
                var code = model.ReferralCode.Trim();
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.ReferralCode == code);
                if (company != null)
                {
                    patient.CompanyId = company.CompanyId;
                }
                else
                {
                    ModelState.AddModelError("ReferralCode", "Kode referral tidak ditemukan.");
                    ViewBag.ActiveNav = "Profil";
                    return View(model);
                }
            }

            // Notification preferences (create if missing)
            var prefs = await _context.PatientNotificationPreferences
                .FirstOrDefaultAsync(n => n.PatientId == patient.PatientId);
            if (prefs == null)
            {
                prefs = new PatientNotificationPreference { PatientId = patient.PatientId };
                _context.PatientNotificationPreferences.Add(prefs);
            }
            prefs.RemindMoodCheck = model.RemindMoodCheck;
            prefs.RemindCounselingSession = model.RemindCounselingSession;
            prefs.AllowHrPsychologistNotif = model.AllowHrPsychologistNotif;
            prefs.ReminderTime = model.ReminderTime;

            await _context.SaveChangesAsync();
            TempData["success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }
    }
}
