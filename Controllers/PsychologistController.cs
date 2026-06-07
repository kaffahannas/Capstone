using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LightenUp.Web.Controllers
{
    [Authorize(Roles = "Psychologist")]
    public class PsychologistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;
        private static readonly Random _random = new Random();

        public PsychologistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserUploadService uploads)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
        }

        // ==========================================
        // 1. DASHBOARD (DINAMIS DARI DATABASE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var psychologist = await _context.Psychologists
                .Include(p => p.PartneredCompanies)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (psychologist == null) return NotFound("Data Psikolog tidak ditemukan.");

            // 1A. Klien Aktif (Yang sedang ditangani) — include AssignmentId for cancel modal
            var activeAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psychologist.PsychologistId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .ToListAsync();

            var activePatients = activeAssignments.Select(a => a.Patient!).ToList();

            // 1A-pending. Klien menunggu persetujuan Admin atau psikolog
            var pendingAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psychologist.PsychologistId &&
                            (a.Status == "PendingAdminApproval" || a.Status == "PendingPsychologistApproval"))
                .ToListAsync();

            // 1B. Daftar Perusahaan Mitra
            var partnerCompanies = psychologist.PartneredCompanies.ToList();
            var partnerCompanyIds = partnerCompanies.Select(c => c.CompanyId).ToList();

            // 1C. Klien Tersedia (Belum punya psikolog) -> Publik ATAU Karyawan Mitra
            var unassignedPatientsDb = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .Where(p => !_context.Assignments.Any(a => a.PatientId == p.PatientId &&
                    (a.Status == "Active" || a.Status == "PendingAdminApproval" || a.Status == "PendingPsychologistApproval")))
                .Where(p => p.CompanyId == null || partnerCompanyIds.Contains(p.CompanyId.Value))
                .ToListAsync();

            var viewModel = new PsychologistDashboardViewModel
            {
                PsychologistName = user.FullName ?? "Psikolog",
                TotalClients = activePatients.Count,
                Patients = activeAssignments.Select(a => new PatientListItem
                {
                    PatientId = a.Patient?.PatientId ?? 0,
                    FullName = a.Patient?.User?.FullName ?? "Anonim",
                    Gender = a.Patient?.Gender ?? "-",
                    JoinedDate = a.AssignedAt,
                    Status = a.Patient?.MentalHealthStatus ?? "Sehat",
                    CompanyId = a.Patient?.CompanyId,
                    CompanyName = a.Patient?.Company?.Name ?? "Publik",
                    AssignmentId = a.AssignmentId
                }).ToList(),
                PendingAssignments = pendingAssignments.Select(a => new PatientListItem
                {
                    PatientId = a.Patient?.PatientId ?? 0,
                    FullName = a.Patient?.User?.FullName ?? "Anonim",
                    Gender = a.Patient?.Gender ?? "-",
                    Status = a.Status,
                    CompanyId = a.Patient?.CompanyId,
                    CompanyName = a.Patient?.Company?.Name ?? "Publik",
                    AssignmentId = a.AssignmentId
                }).ToList(),
                PartnerCompanies = partnerCompanies,
                UnassignedPatients = unassignedPatientsDb.Select(p => new PatientListItem
                {
                    PatientId = p.PatientId,
                    FullName = p.User?.FullName ?? "Anonim",
                    Gender = p.Gender ?? "-",
                    Status = p.MentalHealthStatus ?? "Sehat",
                    CompanyId = p.CompanyId,
                    CompanyName = p.Company?.Name ?? "Publik"
                }).ToList()
            };

            return View(viewModel);
        }

        // ==========================================
        // 2. FITUR MASUKKAN KODE REFERRAL PERUSAHAAN
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> JoinCompany(string referralCode)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists
                .Include(p => p.PartneredCompanies)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.ReferralCode == referralCode);

            if (company != null && psych != null)
            {
                if (!psych.PartneredCompanies.Any(c => c.CompanyId == company.CompanyId))
                {
                    psych.PartneredCompanies.Add(company);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. FITUR ASSIGN CLIENT (MODAL)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> AssignClient(int patientId)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            // Prevent duplicate pending/active requests
            var existing = await _context.Assignments.FirstOrDefaultAsync(a =>
                a.PatientId == patientId && a.PsychologistId == psych.PsychologistId &&
                (a.Status == "Active" || a.Status == "PendingAdminApproval"));
            if (existing != null)
            {
                TempData["info"] = "Permintaan untuk klien ini sudah ada atau sedang ditangani.";
                return RedirectToAction("Index");
            }

            var assignment = new PatientPsychologistAssignment
            {
                PatientId = patientId,
                PsychologistId = psych.PsychologistId,
                Status = "PendingAdminApproval",
                AssignedAt = DateTime.Now,
                RequestedByUserId = user.Id,
                RequestedByRole = "Psychologist"
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan penambahan klien dikirim. Menunggu persetujuan Admin.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // FITUR CANCEL ASSIGNMENT (Psikolog membatalkan kemitraan dengan pasien)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CancelAssignment(int assignmentId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return Forbid();

            var assignment = await _context.Assignments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.PsychologistId == psych.PsychologistId && (a.Status == "Active" || a.Status == "PendingCancellation"));
            if (assignment == null) return NotFound();

            var isB2B = assignment.Patient?.CompanyId != null;
            if (isB2B)
            {
                // B2B: need HR approval first
                assignment.Status = "PendingCancellationByHr";
                assignment.CancellationRequestedByUserId = user.Id;
                assignment.CancellationReason = reason;
                assignment.CancellationRequestedAt = DateTime.UtcNow;
                TempData["success"] = "Permintaan pembatalan kemitraan dikirim ke HR untuk disetujui.";
            }
            else
            {
                // B2C: cancel needs Admin approval
                assignment.Status = "PendingCancellationByAdmin";
                assignment.CancellationRequestedByUserId = user.Id;
                assignment.CancellationReason = reason;
                assignment.CancellationRequestedAt = DateTime.UtcNow;
                TempData["success"] = "Permintaan pembatalan kemitraan dikirim ke Admin untuk disetujui.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. DETAIL PASIEN (DINAMIS DARI DATABASE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PatientDetail(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();

            string ageStr = "Belum diatur";
            if (patient.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                var birth = patient.DateOfBirth.Value;
                int age = today.Year - birth.Year;
                if (birth.Date > today.AddYears(-age)) age--;
                ageStr = $"{age} tahun";
            }

            // Today's journal entry (free-write)
            var todayJournal = await _context.Journals
                .Where(j => j.PatientId == id && j.JournalDate.Date == DateTime.Today)
                .OrderByDescending(j => j.UpdatedAt)
                .FirstOrDefaultAsync();

            // ─── Mood data (last 7 days) ───
            var from7 = DateTime.Today.AddDays(-6);
            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == id && m.MoodDate >= from7)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            var chartDates = Enumerable.Range(0, 7).Select(i => from7.AddDays(i)).ToList();
            var chartScores = chartDates.Select(d =>
            {
                var m = moods.FirstOrDefault(x => x.MoodDate.Date == d.Date);
                if (m == null) return (double?)null;
                return (double?)(m.Feeling switch
                {
                    "Overjoyed" => 5, "Happy" => 4, "Calm" => 4,
                    "Neutral" => 3, "Disappointed" => 2, "Angry" => 1, _ => (int?)null
                });
            }).ToList();

            int sehatN = 0, beresikoN = 0, bahayaN = 0;
            foreach (var s in chartScores.Where(x => x.HasValue).Select(x => x!.Value))
            {
                if (s >= 4) sehatN++;
                else if (s >= 2.5) beresikoN++;
                else bahayaN++;
            }
            int totalN = Math.Max(1, sehatN + beresikoN + bahayaN);

            // Today's schedule (if any) and open worksheet count
            var todaySession = await _context.Schedules
                .Where(s => s.PatientId == id && s.SessionStart >= DateTime.Today && s.SessionStart < DateTime.Today.AddDays(1) && s.Status == "Scheduled")
                .OrderBy(s => s.SessionStart)
                .FirstOrDefaultAsync();
            var openWorksheetCount = await _context.Worksheets.CountAsync(w => w.PatientId == id && w.Status != "Completed");

            ViewBag.Symptoms = patient.Symptoms;
            ViewBag.MoodLabels = System.Text.Json.JsonSerializer.Serialize(chartDates.Select(d => d.ToString("dd/MM")));
            ViewBag.MoodScores = System.Text.Json.JsonSerializer.Serialize(chartScores);
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId != null)
            {
                var activeAssignment = await _context.Assignments
                    .Where(a => a.PatientId == id && a.PsychologistId == psyId.Value && (a.Status == "Active" || a.Status == "PendingCancellation"))
                    .FirstOrDefaultAsync();
                ViewBag.AssignmentId = activeAssignment?.AssignmentId;
            }

            ViewBag.SehatPct = (int)Math.Round((double)sehatN / totalN * 100);
            ViewBag.BeresikoPct = (int)Math.Round((double)beresikoN / totalN * 100);
            ViewBag.BahayaPct = (int)Math.Round((double)bahayaN / totalN * 100);
            ViewBag.HasMoodData = moods.Any();
            ViewBag.TodaySession = todaySession;
            ViewBag.OpenWorksheetCount = openWorksheetCount;

            var viewModel = new PatientDetailViewModel
            {
                PatientId = patient.PatientId,
                FullName = patient.User?.FullName ?? "Anonim",
                Gender = patient.Gender == "Male" ? "Laki-laki" : (patient.Gender == "Female" ? "Perempuan" : (patient.Gender ?? "Belum diatur")),
                Age = ageStr,
                Location = patient.Company != null ? (patient.Company.Address ?? patient.Company.Name) : "Pasien Publik",
                Phone = patient.User?.PhoneNumber ?? "-",
                Status = patient.MentalHealthStatus ?? "Sehat",
                JournalContent = string.IsNullOrEmpty(todayJournal?.Content) ? "Belum ada catatan jurnal hari ini." : todayJournal!.Content,
                Complaint = string.IsNullOrEmpty(patient.Symptoms) ? "Tidak ada keluhan" : patient.Symptoms
            };

            return View(viewModel);
        }

        // ==========================================
        // 4b. MOOD DATA (AJAX – period toggle)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetMoodData(int patientId, int days)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var from = DateTime.Today.AddDays(-(days - 1));

            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == patientId && m.MoodDate >= from)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            static double? ScoreMood(string? f) => f switch
            {
                "Overjoyed"    => 5,
                "Happy"        => 4,
                "Calm"         => 4,
                "Neutral"      => 3,
                "Disappointed" => 2,
                "Angry"        => 1,
                _              => (double?)null
            };

            List<string>  labels;
            List<double?> scores;

            if (days <= 30)
            {
                // Daily points
                var dates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
                labels = dates.Select(d => d.ToString("dd/MM")).ToList();
                scores = dates.Select(d =>
                {
                    var m = moods.FirstOrDefault(x => x.MoodDate.Date == d.Date);
                    return m == null ? (double?)null : ScoreMood(m.Feeling);
                }).ToList();
            }
            else
            {
                // Weekly aggregates (~13 data points for 90 days)
                int weeks = (days / 7) + 1;
                var weekStarts = Enumerable.Range(0, weeks).Select(i => from.AddDays(i * 7)).ToList();
                labels = weekStarts.Select(w => w.ToString("dd/MM")).ToList();
                scores = weekStarts.Select(w =>
                {
                    var wm = moods.Where(m => m.MoodDate.Date >= w.Date && m.MoodDate.Date < w.AddDays(7).Date).ToList();
                    if (!wm.Any()) return (double?)null;
                    var vals = wm.Select(m => ScoreMood(m.Feeling)).Where(v => v != null).Select(v => v!.Value).ToList();
                    return vals.Any() ? (double?)vals.Average() : null;
                }).ToList();
            }

            // Distribution percentages (non-null points only)
            int sehatN = 0, beresikoN = 0, bahayaN = 0;
            foreach (var s in scores.Where(x => x.HasValue).Select(x => x!.Value))
            {
                if (s >= 4) sehatN++;
                else if (s >= 2.5) beresikoN++;
                else bahayaN++;
            }
            int totalN = Math.Max(1, sehatN + beresikoN + bahayaN);

            return Json(new
            {
                labels,
                scores,
                sehatPct    = (int)Math.Round((double)sehatN    / totalN * 100),
                beresikoPct = (int)Math.Round((double)beresikoN / totalN * 100),
                bahayaPct   = (int)Math.Round((double)bahayaN   / totalN * 100),
                hasData     = moods.Any()
            });
        }

        // ==========================================
        // 5. PROFIL (FULL DB)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psych = await _context.Psychologists
                .Include(p => p.NotificationPreference)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            // Workload counts
            var activeCases = await _context.Assignments
                .CountAsync(a => a.PsychologistId == psych.PsychologistId && (a.Status == "Active" || a.Status == "PendingCancellation"));
            var employeesCount = await _context.Assignments
                .Where(a => a.PsychologistId == psych.PsychologistId)
                .Select(a => a.PatientId).Distinct().CountAsync();

            var prefs = psych.NotificationPreference;

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
                RemindNewReports = prefs?.RemindNewReports ?? true,
                RemindFollowUp = prefs?.RemindFollowUp ?? true,
                AllowHrPatientNotif = prefs?.AllowHrPatientNotif ?? false,
                Frequency = prefs?.Frequency ?? "Daily"
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(LightenUp.Web.Models.ViewModels.EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            user.FullName    = model.FullName.Trim();
            user.PhoneNumber = model.Phone?.Trim();

            // Handle profile photo file upload
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

        [HttpPost]
        public async Task<IActionResult> SetAvailability(bool isAvailable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych != null)
            {
                psych.IsAvailable = isAvailable;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> SavePrefs(bool remindNewReports, bool remindFollowUp, bool allowHrPatientNotif, string frequency)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            var prefs = await _context.PsyNotificationPreferences.FirstOrDefaultAsync(n => n.PsychologistId == psych.PsychologistId);
            if (prefs == null)
            {
                prefs = new PsyNotificationPreference { PsychologistId = psych.PsychologistId };
                _context.PsyNotificationPreferences.Add(prefs);
            }
            prefs.RemindNewReports = remindNewReports;
            prefs.RemindFollowUp = remindFollowUp;
            prefs.AllowHrPatientNotif = allowHrPatientNotif;
            if (frequency is "Daily" or "Weekly" or "Monthly") prefs.Frequency = frequency;

            await _context.SaveChangesAsync();
            TempData["success"] = "Preferensi disimpan.";
            return RedirectToAction(nameof(Profile));
        }

        // ═════════════════════════════════════════
        //  Add Schedule (real, DB-wired)
        // ═════════════════════════════════════════
        private async Task<List<LightenUp.Web.Models.ViewModels.PsyPatientOption>> LoadPatientOptionsAsync(int psyId)
        {
            return await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellation"))
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Select(a => new LightenUp.Web.Models.ViewModels.PsyPatientOption
                {
                    PatientId = a.PatientId,
                    FullName = a.Patient!.User!.FullName,
                    CompanyName = a.Patient.Company != null ? a.Patient.Company.Name : null
                })
                .Distinct()
                .OrderBy(o => o.FullName)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> AddSchedule(int? patientId = null)
        {
            if (patientId.HasValue)
                return RedirectToAction(nameof(PatientScheduleHistory), new { id = patientId.Value, add = true });
            return RedirectToAction(nameof(Scheduling), new { add = true, patientId });
        }

        // ═════════════════════════════════════════
        //  Edit Schedule (psikolog dapat mengedit jadwal)
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> EditSchedule(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var s = await _context.Schedules
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            var model = new PsyScheduleEditViewModel
            {
                ScheduleId = s.ScheduleId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status,
                Notes = s.Notes,
                MeetingLink = s.MeetingLink
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(PsyScheduleEditViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
                return View(model);

            var s = await _context.Schedules
                .FirstOrDefaultAsync(x => x.ScheduleId == model.ScheduleId && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            s.SessionStart = model.SessionStart;
            s.DurationMinutes = model.DurationMinutes;
            s.Status = model.Status;
            s.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes;
            s.MeetingLink = string.IsNullOrWhiteSpace(model.MeetingLink) ? null : model.MeetingLink;

            await _context.SaveChangesAsync();
            TempData["success"] = "Jadwal sesi berhasil diperbarui.";
            return RedirectToAction(nameof(Scheduling));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSchedule(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var s = await _context.Schedules
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            if (s.Status != "Completed")
            {
                s.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["success"] = "Jadwal berhasil dibatalkan.";
            }

            return RedirectToAction(nameof(Scheduling));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (model.PatientId <= 0)
                ModelState.AddModelError(nameof(model.PatientId), "Pilih pasien.");

            if (!ModelState.IsValid)
            {
                model.AvailablePatients = await LoadPatientOptionsAsync(psyId.Value);
                if (model.ReturnPatientId.HasValue)
                    return await PatientScheduleHistoryViewAsync(model.ReturnPatientId.Value, model, openModal: true);
                return await SchedulingViewAsync(model.ReturnFilter ?? "Semua", model, openModal: true);
            }

            var sessionStart = model.SessionDate.Date.Add(model.SessionTime);
            _context.Schedules.Add(new Schedule
            {
                PsychologistId = psyId.Value,
                PatientId = model.PatientId,
                SessionStart = sessionStart,
                DurationMinutes = model.DurationMinutes,
                Status = "Scheduled",
                Notes = model.Notes,
                MeetingLink = model.MeetingLink
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Jadwal konseling baru ditambahkan.";

            if (model.ReturnPatientId.HasValue)
                return RedirectToAction(nameof(PatientScheduleHistory), new { id = model.ReturnPatientId.Value });
            return RedirectToAction(nameof(Scheduling), new { filter = model.ReturnFilter ?? "Semua" });
        }

        // ═════════════════════════════════════════
        //  Add Task / Worksheet (real, DB-wired)
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> AddTask(int? patientId = null)
        {
            if (patientId.HasValue)
                return RedirectToAction(nameof(PatientWorksheetHistory), new { id = patientId.Value, add = true });
            return RedirectToAction(nameof(Worksheet), new { add = true, patientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (model.PatientId <= 0)
                ModelState.AddModelError(nameof(model.PatientId), "Pilih pasien.");

            if (!ModelState.IsValid)
            {
                model.AvailablePatients = await LoadPatientOptionsAsync(psyId.Value);
                if (model.ReturnPatientId.HasValue)
                    return await PatientWorksheetHistoryViewAsync(model.ReturnPatientId.Value, model, openModal: true);
                return await WorksheetViewAsync(model, openModal: true);
            }

            var deadline = model.DeadlineDate.Date.Add(model.DeadlineTime);
            _context.Worksheets.Add(new Worksheet
            {
                PsychologistId = psyId.Value,
                PatientId = model.PatientId,
                TaskName = model.TaskName,
                Description = model.Description,
                Deadline = deadline,
                Status = "Assigned",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Worksheet baru ditambahkan.";

            if (model.ReturnPatientId.HasValue)
                return RedirectToAction(nameof(PatientWorksheetHistory), new { id = model.ReturnPatientId.Value });
            return RedirectToAction(nameof(Worksheet));
        }

        // ═════════════════════════════════════════
        //  Edit Task / Worksheet
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> EditWorksheet(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psyId.Value);
            
            if (w == null) return NotFound();

            var model = new LightenUp.Web.Models.ViewModels.PsyWorksheetEditViewModel
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Description = w.Description,
                DeadlineDate = w.Deadline.Date,
                DeadlineTime = w.Deadline.TimeOfDay
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorksheet(LightenUp.Web.Models.ViewModels.PsyWorksheetEditViewModel model)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
                return View(model);

            var w = await _context.Worksheets
                .FirstOrDefaultAsync(x => x.WorksheetId == model.WorksheetId && x.PsychologistId == psyId.Value);
            
            if (w == null) return NotFound();

            w.TaskName = model.TaskName;
            w.Description = model.Description;
            w.Deadline = model.DeadlineDate.Date.Add(model.DeadlineTime);

            await _context.SaveChangesAsync();
            TempData["success"] = "Worksheet berhasil diperbarui.";
            return RedirectToAction(nameof(Worksheet));
        }

        // ==========================================
        // 6. PAGES — all DB-driven now (no hardcoded data)
        // ==========================================

        // Map internal status enum → UI label + CSS class
        private static (string Label, string Css) MapStatus(string dbStatus) => dbStatus switch
        {
            "Assigned"   => ("Belum Dikerjakan", "belum"),
            "InProgress" => ("Review",            "review"),
            "Completed"  => ("Selesai",           "selesai"),
            _            => (dbStatus,             "")
        };

        private async Task<int?> CurrentPsychologistIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.PsychologistId).FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            // Distinct patients assigned to this psychologist (active only)
            var assignedIds = await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .Select(a => a.PatientId)
                .Distinct()
                .ToListAsync();

            var patients = await _context.Patients
                .Where(p => assignedIds.Contains(p.PatientId))
                .ToListAsync();

            var viewModel = new StatisticsViewModel
            {
                TotalClients = patients.Count,
                HealthyCount = patients.Count(p => p.MentalHealthStatus == "Sehat"),
                AtRiskCount  = patients.Count(p => p.MentalHealthStatus == "Beresiko"),
                DangerCount  = patients.Count(p => p.MentalHealthStatus == "Bahaya")
            };

            // Per-company breakdown for bar chart + ringkasan cards.
            var byCompany = patients.Where(p => p.CompanyId != null)
                .GroupBy(p => p.CompanyId!.Value)
                .ToList();
            var companyIds = byCompany.Select(g => g.Key).ToList();
            var companyMap = await _context.Companies
                .Where(c => companyIds.Contains(c.CompanyId))
                .ToDictionaryAsync(c => c.CompanyId, c => c.Name);

            var divisions = byCompany.Select(g =>
            {
                var total = g.Count();
                var s = g.Count(p => p.MentalHealthStatus == "Sehat");
                var b = g.Count(p => p.MentalHealthStatus == "Beresiko");
                var d = g.Count(p => p.MentalHealthStatus == "Bahaya");
                return new DivisionRow
                {
                    CompanyId = g.Key,
                    Name = companyMap.GetValueOrDefault(g.Key, "—"),
                    Total = total,
                    SehatPct = total == 0 ? 0 : (int)Math.Round((double)s / total * 100),
                    StressPct = total == 0 ? 0 : (int)Math.Round((double)(b + d) / total * 100)
                };
            }).OrderBy(x => x.Name).ToList();

            ViewBag.Divisions = divisions;
            return View(viewModel);
        }

        public class DivisionRow
        {
            public int CompanyId { get; set; }
            public string Name { get; set; } = "";
            public int Total { get; set; }
            public int SehatPct { get; set; }
            public int StressPct { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Worksheet(bool add = false, int? patientId = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value)
            };
            if (patientId.HasValue) addForm.PatientId = patientId.Value;

            return await WorksheetViewAsync(addForm, openModal: add);
        }

        private async Task<IActionResult> WorksheetViewAsync(
            LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel? addForm = null,
            bool openModal = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var rows = await _context.Worksheets
                .Include(w => w.Patient).ThenInclude(p => p!.User)
                .Where(w => w.PsychologistId == psyId)
                .OrderByDescending(w => w.CreatedAt)
                .Take(50)
                .ToListAsync();

            var tasks = rows.Select(w =>
            {
                var (label, css) = MapStatus(w.Status);
                return new WorksheetItemViewModel
                {
                    TaskId = w.WorksheetId,
                    PatientName = w.Patient?.User?.FullName ?? "—",
                    Date = w.Deadline.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
                    TaskName = w.TaskName,
                    Status = label,
                    StatusClass = css
                };
            }).ToList();

            ViewBag.AddTaskForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value)
            };
            ViewBag.OpenAddTaskModal = openModal;

            return View(new WorksheetViewModel
            {
                TotalActivities = rows.Count,
                Tasks = tasks
            });
        }

        [HttpGet]
        public IActionResult WorksheetDetail(int id)
        {
            // Consolidated into ReviewWorksheet which has the full DB-driven review flow
            // (mark Complete + give feedback). Old WorksheetDetail.cshtml had hardcoded data.
            return RedirectToAction(nameof(ReviewWorksheet), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> WorksheetDetailModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psyId);
            
            if (w == null) return NotFound();

            var model = new LightenUp.Web.Models.ViewModels.PsyWorksheetReviewViewModel
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Description = w.Description,
                ProofImagePath = w.ProofImagePath,
                PatientNote = w.Note,
                Status = w.Status,
                PsychologistFeedback = w.PsychologistFeedback
            };

            return PartialView("_WorksheetDetailModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Scheduling(string filter = "Semua", bool add = false, int? patientId = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                ReturnFilter = filter
            };
            if (patientId.HasValue) addForm.PatientId = patientId.Value;

            return await SchedulingViewAsync(filter, addForm, openModal: add);
        }

        private async Task<IActionResult> SchedulingViewAsync(
            string filter,
            LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel? addForm = null,
            bool openModal = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var today = DateTime.Today;
            var monthEnd = today.AddDays(60);

            var q = _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Patient).ThenInclude(p => p!.Company)
                .Where(s => s.PsychologistId == psyId && s.SessionStart >= today.AddDays(-30) && s.SessionStart < monthEnd);

            var allSessionsInWindow = await q.OrderBy(s => s.SessionStart).ToListAsync();

            var now = DateTime.Now;
            var sessions = filter switch
            {
                "Selesai" => allSessionsInWindow.Where(s => s.Status == "Completed" || (s.Status == "Scheduled" && s.SessionStart.AddMinutes(s.DurationMinutes) <= now)).ToList(),
                "Dibatalkan" => allSessionsInWindow.Where(s => s.Status == "Cancelled").ToList(),
                _ => allSessionsInWindow
            };

            ViewBag.AllSessionsInWindow = allSessionsInWindow;
            ViewBag.Today = today;
            ViewBag.Filter = filter;
            ViewBag.Sessions = sessions;
            ViewBag.AddScheduleForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                ReturnFilter = filter
            };
            ViewBag.OpenAddScheduleModal = openModal;
            return View("Scheduling");
        }

        [HttpGet]
        public async Task<IActionResult> ScheduleHistory()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var sessions = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Where(s => s.PsychologistId == psyId)
                .OrderByDescending(s => s.SessionStart)
                .Take(50)
                .ToListAsync();

            ViewBag.Sessions = sessions;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ScheduleDetailModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var schedule = await _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Patient).ThenInclude(p => p!.Company)
                .FirstOrDefaultAsync(s => s.ScheduleId == id && s.PsychologistId == psyId);

            if (schedule == null) return NotFound();

            return PartialView("_ScheduleDetailModal", schedule);
        }

        [HttpGet]
        public async Task<IActionResult> EditScheduleModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var s = await _context.Schedules
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.ScheduleId == id && x.PsychologistId == psyId.Value);
            if (s == null) return NotFound();

            var model = new PsyScheduleEditViewModel
            {
                ScheduleId = s.ScheduleId,
                PatientName = s.Patient?.User?.FullName ?? "—",
                SessionStart = s.SessionStart,
                DurationMinutes = s.DurationMinutes,
                Status = s.Status,
                Notes = s.Notes,
                MeetingLink = s.MeetingLink
            };

            return PartialView("_EditScheduleModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditWorksheetModal(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psyId.Value);
            
            if (w == null) return NotFound();

            var model = new LightenUp.Web.Models.ViewModels.PsyWorksheetEditViewModel
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Description = w.Description,
                DeadlineDate = w.Deadline.Date,
                DeadlineTime = w.Deadline.TimeOfDay
            };

            return PartialView("_EditWorksheetModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> WorksheetHistory()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var worksheets = await _context.Worksheets
                .Include(w => w.Patient).ThenInclude(p => p!.User)
                .Where(w => w.PsychologistId == psyId)
                .OrderByDescending(w => w.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.Worksheets = worksheets;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PatientScheduleHistory(int id, bool add = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return NotFound();

            var sessions = await _context.Schedules
                .Where(s => s.PsychologistId == psyId && s.PatientId == id)
                .OrderByDescending(s => s.SessionStart)
                .ToListAsync();

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = id,
                ReturnPatientId = id
            };

            return await PatientScheduleHistoryViewAsync(id, addForm, openModal: add, patientName: patient.User?.FullName ?? "—", sessions: sessions);
        }

        private async Task<IActionResult> PatientScheduleHistoryViewAsync(
            int patientId,
            LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel? addForm = null,
            bool openModal = false,
            string? patientName = null,
            List<Schedule>? sessions = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (patientName == null || sessions == null)
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);
                if (patient == null) return NotFound();
                patientName = patient.User?.FullName ?? "—";
                sessions = await _context.Schedules
                    .Where(s => s.PsychologistId == psyId && s.PatientId == patientId)
                    .OrderByDescending(s => s.SessionStart)
                    .ToListAsync();
            }

            ViewBag.PatientName = patientName;
            ViewBag.PatientId = patientId;
            ViewBag.Sessions = sessions;
            ViewBag.AddScheduleForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddScheduleViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = patientId,
                ReturnPatientId = patientId
            };
            ViewBag.OpenAddScheduleModal = openModal;
            return View("PatientScheduleHistory");
        }

        [HttpGet]
        public async Task<IActionResult> PatientWorksheetHistory(int id, bool add = false)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return NotFound();

            var worksheets = await _context.Worksheets
                .Where(w => w.PsychologistId == psyId && w.PatientId == id)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var addForm = new LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = id,
                ReturnPatientId = id
            };

            return await PatientWorksheetHistoryViewAsync(
                id,
                addForm,
                openModal: add,
                patientName: patient.User?.FullName ?? "—",
                worksheets: worksheets);
        }

        private async Task<IActionResult> PatientWorksheetHistoryViewAsync(
            int patientId,
            LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel? addForm = null,
            bool openModal = false,
            string? patientName = null,
            List<Worksheet>? worksheets = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            if (patientName == null || worksheets == null)
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientId == patientId);
                if (patient == null) return NotFound();
                patientName = patient.User?.FullName ?? "—";
                worksheets = await _context.Worksheets
                    .Where(w => w.PsychologistId == psyId && w.PatientId == patientId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();
            }

            ViewBag.PatientName = patientName;
            ViewBag.PatientId = patientId;
            ViewBag.Worksheets = worksheets;
            ViewBag.AddTaskForm = addForm ?? new LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel
            {
                AvailablePatients = await LoadPatientOptionsAsync(psyId.Value),
                PatientId = patientId,
                ReturnPatientId = patientId
            };
            ViewBag.OpenAddTaskModal = openModal;
            return View("PatientWorksheetHistory");
        }

        [HttpGet]
        public async Task<IActionResult> CompanyDetail(int? id, string? companyName)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            Company? company = null;
            if (id.HasValue)
                company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id.Value);
            else if (!string.IsNullOrWhiteSpace(companyName))
                company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);

            if (company == null) return NotFound();

            // Active patients in this company that THIS psychologist is assigned to.
            var assignedPatientIds = await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .Select(a => a.PatientId)
                .ToListAsync();

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == company.CompanyId && assignedPatientIds.Contains(p.PatientId) && p.EmploymentStatus == "active")
                .ToListAsync();

            ViewBag.CompanyName = company.Name;
            ViewBag.Company = company;
            ViewBag.Patients = patients;
            ViewBag.SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CompanyDetailModal(string companyName)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);
            if (company == null) return NotFound();

            var assignedPatientIds = await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .Select(a => a.PatientId)
                .ToListAsync();

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == company.CompanyId && assignedPatientIds.Contains(p.PatientId) && p.EmploymentStatus == "active")
                .ToListAsync();

            ViewBag.CompanyName = company.Name;
            ViewBag.Company = company;
            ViewBag.Patients = patients;
            ViewBag.SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya");
            return PartialView("_CompanyDetailModal");
        }

        [HttpGet]
        public async Task<IActionResult> CompanyStatsModal(string companyName)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);
            if (company == null) return NotFound();

            var assignedPatientIds = await _context.Assignments
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .Select(a => a.PatientId)
                .ToListAsync();

            var patients = await _context.Patients
                .Where(p => p.CompanyId == company.CompanyId && assignedPatientIds.Contains(p.PatientId) && p.EmploymentStatus == "active")
                .ToListAsync();

            ViewBag.CompanyName = company.Name;
            ViewBag.SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya");

            return PartialView("_CompanyStatsModal");
        }

        // ═════════════════════════════════════════
        //  Stub: Report patient to HR (full impl is Psy slice 9, future)
        // ═════════════════════════════════════════
        [HttpGet]
        public IActionResult ReportToHr(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View();
        }

        // ═════════════════════════════════════════
        //  Bridge: Worksheet review (mark Completed + give feedback)
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ReviewWorksheet(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psy.PsychologistId);
            if (w == null) return NotFound();

            return View(new LightenUp.Web.Models.ViewModels.PsyWorksheetReviewViewModel
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Description = w.Description,
                ProofImagePath = w.ProofImagePath,
                PatientNote = w.Note,
                Status = w.Status,
                PsychologistFeedback = w.PsychologistFeedback
            });
        }

        [HttpPost]
        public async Task<IActionResult> ReviewWorksheet(LightenUp.Web.Models.ViewModels.PsyWorksheetReviewViewModel model, string action)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            var w = await _context.Worksheets
                .FirstOrDefaultAsync(x => x.WorksheetId == model.WorksheetId && x.PsychologistId == psy.PsychologistId);
            if (w == null) return NotFound();

            w.PsychologistFeedback = string.IsNullOrWhiteSpace(model.PsychologistFeedback) ? null : model.PsychologistFeedback;

            if (action == "Complete")
            {
                w.Status = "Completed";
                w.ReviewedAt = DateTime.Now;
                TempData["success"] = "Worksheet diselesaikan.";
            }
            else if (action == "Reopen")
            {
                w.Status = "Assigned";
                w.ReviewedAt = null;
                TempData["success"] = "Worksheet dikembalikan ke pasien.";
            }
            else
            {
                TempData["success"] = "Catatan disimpan.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Worksheet));
        }

        // ═════════════════════════════════════════
        //  Bridge: Settings — AcceptsB2B toggle (visibility in HR directory)
        // ═════════════════════════════════════════
        [HttpGet]
        public IActionResult Settings()
        {
            // Settings merged into Profile page
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> Settings(LightenUp.Web.Models.ViewModels.PsySettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            psy.AcceptsB2B = model.AcceptsB2B;
            await _context.SaveChangesAsync();
            TempData["success"] = "Pengaturan B2B disimpan.";
            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // PAYSLIP — Psikolog melihat ringkasan pendapatan
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Payslip()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            var activeAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Where(a => a.PsychologistId == psych.PsychologistId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .ToListAsync();

            var grossThisMonth = activeAssignments.Sum(a => a.SlotValue ?? 0);
            var psyShareThisMonth = activeAssignments.Sum(a =>
                (a.SlotValue ?? 0) * (a.PsychologistRevenuePercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage) / 100);

            var assignmentRows = activeAssignments.Select(a => new PayslipAssignmentRow
            {
                PatientName = a.Patient?.User?.FullName ?? "—",
                SlotValue = a.SlotValue ?? 0,
                Percentage = a.PsychologistRevenuePercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage,
                Earning = (a.SlotValue ?? 0) * (a.PsychologistRevenuePercentage ?? SubscriptionPricingService.DefaultPsychologistRevenuePercentage) / 100,
                IsB2B = a.Patient?.CompanyId != null
            }).OrderBy(r => r.PatientName).ToList();

            ViewBag.PsyName = user.FullName;
            ViewBag.AssignmentRows = assignmentRows;
            ViewBag.ActivePatientCount = activeAssignments.Count;
            ViewBag.GrossThisMonth = grossThisMonth;
            ViewBag.PsyShareThisMonth = psyShareThisMonth;
            ViewBag.MonthLabel = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            ViewBag.HasSnapshots = activeAssignments.Any(a => a.SlotValue != null);

            return View();
        }

        // ==========================================
        // HELPER
        // ==========================================
        private static string GetRandomStatus()
        {
            var statuses = new[] { "Sehat", "Beresiko", "Bahaya" };
            return statuses[_random.Next(statuses.Length)];
        }
    }

    // ==========================================
    // VIEW MODELS (DIPERTAHANKAN 100%)
    // ==========================================

    public class PsychologistDashboardViewModel
    {
        public string PsychologistName { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public List<PatientListItem> Patients { get; set; } = new List<PatientListItem>();
        public List<PatientListItem> PendingAssignments { get; set; } = new List<PatientListItem>();
        public List<Company> PartnerCompanies { get; set; } = new List<Company>();
        public List<PatientListItem> UnassignedPatients { get; set; } = new List<PatientListItem>();
    }

    public class PatientListItem
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Tambahan untuk Filter
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        // Assignment tracking
        public int AssignmentId { get; set; }
    }

    public class PatientDetailViewModel
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string JournalContent { get; set; } = string.Empty;
        public string Complaint { get; set; } = string.Empty;
    }

    public class StatisticsViewModel
    {
        public int TotalClients { get; set; }
        public int HealthyCount { get; set; }
        public int AtRiskCount { get; set; }
        public int DangerCount { get; set; }
    }

    public class WorksheetViewModel
    {
        public int TotalActivities { get; set; }
        public List<WorksheetItemViewModel> Tasks { get; set; } = new List<WorksheetItemViewModel>();
    }

    public class WorksheetItemViewModel
    {
        public int TaskId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }

    public class PsyScheduleEditViewModel
    {
        public int ScheduleId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        [Required] public DateTime SessionStart { get; set; }
        [Required] public int DurationMinutes { get; set; } = 60;
        [Required] public string Status { get; set; } = "Scheduled";
        public string? Notes { get; set; }
        public string? MeetingLink { get; set; }
    }

    public class WorksheetDetailViewModel
    {
        public int TaskId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string TaskDate { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string PsychologistNote { get; set; } = string.Empty;
    }

    public class PsychologistProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string LastDegree { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public string PracticeLocation { get; set; } = string.Empty;
        public string SiapNumber { get; set; } = string.Empty;
        public string SippNumber { get; set; } = string.Empty;
    }

    public class PayslipAssignmentRow
    {
        public string PatientName { get; set; } = string.Empty;
        public decimal SlotValue { get; set; }
        public decimal Percentage { get; set; }
        public decimal Earning { get; set; }
        public bool IsB2B { get; set; }
    }
}
