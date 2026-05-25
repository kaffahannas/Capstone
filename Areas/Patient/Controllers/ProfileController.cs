using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
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
        private readonly UserUploadService _uploads;
        private readonly SubscriptionAccessService _access;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            HealthStatusService healthService,
            UserUploadService uploads,
            SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _healthService = healthService;
            _uploads = uploads;
            _access = access;
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

            // Assigned psychologist
            var psyRecord = await _context.Assignments
                .Where(a => a.PatientId == patient.PatientId && a.Status == "Active")
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new { Name = a.Psychologist!.User!.FullName, Email = a.Psychologist!.User!.Email })
                .FirstOrDefaultAsync();

            // Stats
            var totalSessionsDone = await _context.Schedules
                .CountAsync(s => s.PatientId == patient.PatientId && s.Status == "Completed");
            var totalTasksDone = await _context.Worksheets
                .CountAsync(w => w.PatientId == patient.PatientId && w.Status == "Completed");

            // Mood streak — count consecutive days ending today that have a mood entry
            var allMoodDates = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId)
                .Select(m => m.MoodDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();
            int moodStreak = 0;
            var checkDay = DateTime.Today;
            foreach (var d in allMoodDates)
            {
                if (d == checkDay) { moodStreak++; checkDay = checkDay.AddDays(-1); }
                else break;
            }

            var prefs = patient.NotificationPreference ?? new PatientNotificationPreference { PatientId = patient.PatientId };

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
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactRelation = patient.EmergencyContactRelation,

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
                PsychologistName = psyRecord?.Name,
                PsychologistEmail = psyRecord?.Email,

                TotalSessionsDone = totalSessionsDone,
                TotalTasksDone = totalTasksDone,
                MoodStreakDays = moodStreak
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

            var vm = new PatientProfileEditViewModel
            {
                FullName = user.FullName,
                CurrentProfilePicture = user.ProfilePicture,
                Phone = user.PhoneNumber,
                Department = patient.Department,
                EmployeeId = patient.EmployeeId,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactEmail = patient.EmergencyContactEmail,
                EmergencyContactRelation = patient.EmergencyContactRelation,
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
            var (user, patient) = await LoadAsync();
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient == null) return RedirectToAction("Welcome", "Onboarding");

            if (!ModelState.IsValid)
            {
                model.CurrentProfilePicture = user.ProfilePicture;
                model.IsAlreadyB2B = patient.CompanyId != null;
                model.CurrentCompanyName = patient.Company?.Name;
                ViewBag.ActiveNav = "Profil";
                return View(model);
            }

            // ApplicationUser fields
            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;

            // Profile photo upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var path = await _uploads.ReplaceAsync(
                    user.Id, UserUploadService.Categories.Profile, model.ProfilePicture,
                    user.ProfilePicture, allowedExtensions: UserUploadService.ProfileExtensions);
                if (path != null) user.ProfilePicture = path;
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

            // Referral code → assign company and division if valid and patient isn't already B2B
            if (!string.IsNullOrWhiteSpace(model.ReferralCode) && patient.CompanyId == null)
            {
                var code = model.ReferralCode.Trim();
                var division = await _context.CompanyDivisions.FirstOrDefaultAsync(c => c.ReferralCode == code);
                if (division != null)
                {
                    if (!await _access.CanUseReferralCodeAsync(division.CompanyId))
                    {
                        ModelState.AddModelError("ReferralCode", "Langganan perusahaan belum aktif atau sudah berakhir. Hubungi HR perusahaan Anda.");
                        model.CurrentProfilePicture = user.ProfilePicture;
                        model.IsAlreadyB2B = patient.CompanyId != null;
                        model.CurrentCompanyName = patient.Company?.Name;
                        ViewBag.ActiveNav = "Profil";
                        return View(model);
                    }
                    patient.CompanyId = division.CompanyId;
                    patient.Department = division.Name;
                }
                else
                {
                    ModelState.AddModelError("ReferralCode", "Kode referral tidak ditemukan.");
                    model.CurrentProfilePicture = user.ProfilePicture;
                    model.IsAlreadyB2B = patient.CompanyId != null;
                    model.CurrentCompanyName = patient.Company?.Name;
                    ViewBag.ActiveNav = "Profil";
                    return View(model);
                }
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Profil berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }
    }
}
