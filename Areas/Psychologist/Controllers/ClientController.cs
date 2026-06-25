using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    [Authorize(Roles = "Psychologist")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int?> CurrentPsychologistIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.PsychologistId).FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Login", "Account", new { area = "" });

            var activeAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psyId && (a.Status == "Active" || a.Status == "PendingCancellationByHr" || a.Status == "PendingCancellationByAdmin"))
                .ToListAsync();

            // Batch-check subscription status (hindari N+1 queries)
            var b2cPatientIds = activeAssignments
                .Where(a => a.Patient?.CompanyId == null && a.Patient?.SponsorType != "Psychologist")
                .Select(a => a.Patient!.PatientId).ToList();

            var activeB2cSubIds = (await _context.Subscriptions
                .Where(s => s.Status == "Active" && s.EndDate >= DateTime.Today
                    && s.PsychologistId == psyId && b2cPatientIds.Contains(s.PatientId))
                .Select(s => s.PatientId).ToListAsync()).ToHashSet();

            var companyIds = activeAssignments
                .Where(a => a.Patient?.CompanyId != null)
                .Select(a => a.Patient!.CompanyId!.Value).Distinct().ToList();

            var activeCompanyIds = (await _context.CompanySubscriptions
                .Where(s => s.Status == "Active" && s.EndDate >= DateTime.Today
                    && companyIds.Contains(s.CompanyId))
                .Select(s => s.CompanyId).ToListAsync()).ToHashSet();

            var patients = activeAssignments.Select(a =>
            {
                bool expired;
                if (a.Patient?.CompanyId != null)
                    expired = !activeCompanyIds.Contains(a.Patient.CompanyId.Value);
                else if (a.Patient?.SponsorType == "Psychologist")
                    expired = false; // mitra yang expired sudah lazy-cancelled
                else
                    expired = !activeB2cSubIds.Contains(a.Patient?.PatientId ?? 0);

                return new LightenUp.Web.Models.ViewModels.PatientListItem
                {
                    PatientId = a.Patient?.PatientId ?? 0,
                    FullName = a.Patient?.User?.FullName ?? "Anonim",
                    Gender = a.Patient?.Gender == "Male" ? "Laki-laki" : (a.Patient?.Gender == "Female" ? "Perempuan" : "Belum diatur"),
                    JoinedDate = a.AssignedAt,
                    Status = a.Patient?.MentalHealthStatus ?? "Sehat",
                    CompanyId = a.Patient?.CompanyId,
                    CompanyName = a.Patient?.Company?.Name ?? "Klien Publik",
                    AssignmentId = a.AssignmentId,
                    IsSubscriptionExpired = expired,
                    SponsorType = a.Patient?.SponsorType ?? "Self"
                };
            }).ToList();

            return View(patients);
        }


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
                assignment.Status = "PendingCancellationByHr";
                assignment.CancellationRequestedByUserId = user.Id;
                assignment.CancellationReason = reason;
                assignment.CancellationRequestedAt = DateTime.UtcNow;
                TempData["success"] = "Permintaan pembatalan kemitraan dikirim ke HR untuk disetujui.";
            }
            else
            {
                assignment.Status = "PendingCancellationByAdmin";
                assignment.CancellationRequestedByUserId = user.Id;
                assignment.CancellationReason = reason;
                assignment.CancellationRequestedAt = DateTime.UtcNow;
                TempData["success"] = "Permintaan pembatalan kemitraan dikirim ke Admin untuk disetujui.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }

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

            var todayJournal = await _context.Journals
                .Where(j => j.PatientId == id && j.JournalDate.Date == DateTime.Today)
                .OrderByDescending(j => j.UpdatedAt)
                .FirstOrDefaultAsync();

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

            var viewModel = new LightenUp.Web.Models.ViewModels.PatientDetailViewModel
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

        [HttpGet]
        public async Task<IActionResult> CompanyDetail(int? id, string? companyName)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            Company? company = null;
            if (id.HasValue)
                company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id.Value);
            else if (!string.IsNullOrWhiteSpace(companyName))
                company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);

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

        [HttpGet]
        public IActionResult ReportToHr(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View();
        }
    }
}
