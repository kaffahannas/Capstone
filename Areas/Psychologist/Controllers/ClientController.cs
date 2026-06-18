using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
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
    // #Class ClientController#
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HealthStatusService _healthService;
        private readonly AssignmentActivationService _activation;

        public ClientController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            HealthStatusService healthService,
            AssignmentActivationService activation)
        {
            _context = context;
            _userManager = userManager;
            _healthService = healthService;
            _activation = activation;
        }

        // #Function CurrentPsychologistIdAsync#
        private async Task<int?> CurrentPsychologistIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.Where(p => p.UserId == user.Id)
                .Select(p => (int?)p.PsychologistId).FirstOrDefaultAsync();
        }

        // #Bagian Daftar Klien#
        // #Function Index#
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Login", "Account", new { area = "" });

            await _activation.RepairDuplicateLiveAssignmentsAsync(psyId.Value);

            var activeAssignments = AssignmentActivationService.SelectPrimaryPerPatient(
                await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Where(a => a.PsychologistId == psyId && AssignmentActivationService.LiveClientListStatuses.Contains(a.Status))
                .ToListAsync());

            await _healthService.RefreshStatusesAsync(activeAssignments.Select(a => a.Patient!));

            var patients = activeAssignments.Select(a => new LightenUp.Web.Models.ViewModels.PatientListItem
            {
                PatientId = a.Patient?.PatientId ?? 0,
                FullName = a.Patient?.User?.FullName ?? "Anonim",
                Gender = a.Patient?.Gender == "Male" ? "Laki-laki" : (a.Patient?.Gender == "Female" ? "Perempuan" : "Belum diatur"),
                JoinedDate = a.AssignedAt,
                Status = a.Patient?.MentalHealthStatus ?? "Sehat",
                CompanyId = a.Patient?.CompanyId,
                CompanyName = a.Patient?.Company?.Name ?? "Klien Publik",
                AssignmentId = a.AssignmentId
            }).ToList();

            return View(patients);
        }


        // #Function AssignClient#
        [HttpPost]
        public async Task<IActionResult> AssignClient(int patientId)
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psych == null) return NotFound();

            var existing = await _activation.HasLiveAssignmentForPairAsync(patientId, psych.PsychologistId);
            if (existing)
            {
                TempData["info"] = "Permintaan untuk klien ini sudah ada atau sedang ditangani.";
                return RedirectToAction("Index", "Dashboard");
            }

            var assignment = new PatientPsychologistAssignment
            {
                PatientId = patientId,
                PsychologistId = psych.PsychologistId,
                Status = "PendingAdminApproval",
                AssignedAt = DateTime.UtcNow,
                RequestedByUserId = user.Id,
                RequestedByRole = "Psychologist"
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan penambahan klien dikirim. Menunggu persetujuan Admin.";
            return RedirectToAction("Index", "Dashboard");
        }

        // #Function CancelAssignment#
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

        // #Bagian Detail Klien#
        // #Function PatientDetail#
        [HttpGet]
        public async Task<IActionResult> PatientDetail(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();

            await _healthService.UpdateAndSaveAsync(patient);
            var snap = await _healthService.ComputeAsync(patient.PatientId);
            var moodWindow = await _healthService.ComputeMoodWindowAsync(id, 7);

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

            var todaySession = await _context.Schedules
                .Where(s => s.PatientId == id && s.SessionStart >= DateTime.Today && s.SessionStart < DateTime.Today.AddDays(1) && s.Status == "Scheduled")
                .OrderBy(s => s.SessionStart)
                .FirstOrDefaultAsync();
            var openWorksheetCount = await _context.Worksheets.CountAsync(w => w.PatientId == id && w.Status != "Completed");

            ViewBag.Symptoms = patient.Symptoms;
            ViewBag.MoodLabels = System.Text.Json.JsonSerializer.Serialize(moodWindow.Labels);
            ViewBag.MoodScores = System.Text.Json.JsonSerializer.Serialize(moodWindow.Scores);
            
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId != null)
            {
                var activeAssignment = await _context.Assignments
                    .Where(a => a.PatientId == id && a.PsychologistId == psyId.Value && AssignmentActivationService.LiveClientListStatuses.Contains(a.Status))
                    .ToListAsync();
                if (activeAssignment.Count == 0)
                    ViewBag.AssignmentId = null;
                else
                {
                    var primary = AssignmentActivationService.SelectPrimaryAssignment(activeAssignment);
                    ViewBag.AssignmentId = (int?)primary.AssignmentId;
                }
            }

            ViewBag.SehatPct = moodWindow.Distribution.SehatPct;
            ViewBag.BeresikoPct = moodWindow.Distribution.BeresikoPct;
            ViewBag.BahayaPct = moodWindow.Distribution.BahayaPct;
            ViewBag.HasMoodData = moodWindow.HasData;
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
                Status = snap.Status,
                JournalContent = string.IsNullOrEmpty(todayJournal?.Content) ? "Belum ada catatan jurnal hari ini." : todayJournal!.Content,
                Complaint = string.IsNullOrEmpty(patient.Symptoms) ? "Tidak ada keluhan" : patient.Symptoms
            };

            return View(viewModel);
        }

        // #Function GetMoodData#
        [HttpGet]
        public async Task<IActionResult> GetMoodData(int patientId, int days)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return Unauthorized();

            var moodWindow = await _healthService.ComputeMoodWindowAsync(patientId, days);

            return Json(new
            {
                labels = moodWindow.Labels,
                scores = moodWindow.Scores,
                sehatPct = moodWindow.Distribution.SehatPct,
                beresikoPct = moodWindow.Distribution.BeresikoPct,
                bahayaPct = moodWindow.Distribution.BahayaPct,
                hasData = moodWindow.HasData
            });
        }

        // #Bagian Detail Perusahaan#
        // #Function CompanyDetail#
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

            await _healthService.RefreshStatusesAsync(patients);

            ViewBag.CompanyName = company.Name;
            ViewBag.Company = company;
            ViewBag.Patients = patients;
            ViewBag.SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya");
            return View();
        }

        // #Function CompanyDetailModal#
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

            await _healthService.RefreshStatusesAsync(patients);

            ViewBag.CompanyName = company.Name;
            ViewBag.Company = company;
            ViewBag.Patients = patients;
            ViewBag.SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat");
            ViewBag.BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko");
            ViewBag.BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya");
            return PartialView("_CompanyDetailModal");
        }

        // #Function CompanyStatsModal#
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

        // #Function ReportToHr#
        [HttpGet]
        public IActionResult ReportToHr(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View();
        }
    }
}
