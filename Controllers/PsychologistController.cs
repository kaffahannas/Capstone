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

namespace LightenUp.Web.Controllers
{
    [Authorize]
    public class PsychologistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly Random _random = new Random();

        public PsychologistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // 1A. Klien Aktif (Yang sedang ditangani)
            var assignedPatientIds = await _context.Assignments
                .Where(a => a.PsychologistId == psychologist.PsychologistId && a.Status == "Active")
                .Select(a => a.PatientId)
                .ToListAsync();

            var activePatients = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .Where(p => assignedPatientIds.Contains(p.PatientId))
                .ToListAsync();

            // 1B. Daftar Perusahaan Mitra
            var partnerCompanies = psychologist.PartneredCompanies.ToList();
            var partnerCompanyIds = partnerCompanies.Select(c => c.CompanyId).ToList();

            // 1C. Klien Tersedia (Belum punya psikolog) -> Publik ATAU Karyawan Mitra
            var unassignedPatientsDb = await _context.Patients
                .Include(p => p.User)
                .Include(p => p.Company)
                .Where(p => !_context.Assignments.Any(a => a.PatientId == p.PatientId && a.Status == "Active"))
                .Where(p => p.CompanyId == null || partnerCompanyIds.Contains(p.CompanyId.Value))
                .ToListAsync();

            var viewModel = new PsychologistDashboardViewModel
            {
                PsychologistName = user.FullName ?? "Psikolog",
                TotalClients = activePatients.Count,
                Patients = activePatients.Select(p => new PatientListItem
                {
                    PatientId = p.PatientId,
                    FullName = p.User?.FullName ?? "Anonim",
                    Gender = p.Gender ?? "-",
                    JoinedDate = DateTime.Now.AddMonths(-1),
                    Status = p.MentalHealthStatus ?? "Sehat"
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

            var assignment = new PatientPsychologistAssignment
            {
                PatientId = patientId,
                PsychologistId = psych.PsychologistId,
                Status = "Active",
                AssignedAt = DateTime.Now
            };

            _context.Assignments.Add(assignment);
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

            var viewModel = new PatientDetailViewModel
            {
                PatientId = patient.PatientId,
                FullName = patient.User?.FullName ?? "Anonim",
                Gender = patient.Gender ?? "Belum diatur",
                Age = ageStr,
                Location = patient.Company != null ? patient.Company.Address : "Pasien Publik",
                Phone = patient.User?.PhoneNumber ?? "-",
                Status = patient.MentalHealthStatus ?? "Sehat",
                JournalContent = "Belum ada catatan jurnal terbaru.",
                Complaint = "Tidak ada keluhan"
            };

            return View(viewModel);
        }

        // ==========================================
        // 5. PROFIL (DINAMIS DARI DATABASE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var psych = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var viewModel = new PsychologistProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                ProfilePicture = user.ProfilePicture,
                Specialization = psych?.Specialization ?? "Belum diatur",
                LastDegree = psych?.LastDegree ?? "Belum diatur",
                University = psych?.University ?? "Belum diatur",
                PracticeLocation = psych?.PracticeLocation ?? "Belum diatur",
                SiapNumber = psych?.SiapNumber ?? "-",
                SippNumber = psych?.LicenseNumber ?? "-"
            };

            return View(viewModel);
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
                .Where(a => a.PsychologistId == psyId && a.Status == "Active")
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
        public async Task<IActionResult> Worksheet()
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

            return View(new WorksheetViewModel
            {
                TotalActivities = rows.Count,
                Tasks = tasks
            });
        }

        [HttpGet]
        public async Task<IActionResult> WorksheetDetail(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psyId);
            if (w == null) return NotFound();

            var (label, css) = MapStatus(w.Status);
            return View(new WorksheetDetailViewModel
            {
                TaskId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                Status = label,
                StatusClass = css,
                TaskDate = w.Deadline.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
                TaskName = w.TaskName,
                PsychologistNote = w.PsychologistFeedback ?? string.Empty
            });
        }

        [HttpGet]
        public async Task<IActionResult> Scheduling(string filter = "Semua")
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction(nameof(Index));

            var today = DateTime.Today;
            var monthEnd = today.AddDays(60);  // ~next two months

            var q = _context.Schedules
                .Include(s => s.Patient).ThenInclude(p => p!.User)
                .Include(s => s.Patient).ThenInclude(p => p!.Company)
                .Where(s => s.PsychologistId == psyId && s.SessionStart >= today.AddDays(-30) && s.SessionStart < monthEnd);

            if (filter == "Selesai") q = q.Where(s => s.Status == "Completed");
            else if (filter == "Dibatalkan") q = q.Where(s => s.Status == "Cancelled");

            var sessions = await q.OrderBy(s => s.SessionStart).ToListAsync();

            ViewBag.Today = today;
            ViewBag.Filter = filter;
            ViewBag.Sessions = sessions;
            return View();
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
        public async Task<IActionResult> PatientScheduleHistory(int id)
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

            ViewBag.PatientName = patient.User?.FullName ?? "—";
            ViewBag.PatientId = id;
            ViewBag.Sessions = sessions;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PatientWorksheetHistory(int id)
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

            ViewBag.PatientName = patient.User?.FullName ?? "—";
            ViewBag.PatientId = id;
            ViewBag.Worksheets = worksheets;
            return View();
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
                .Where(a => a.PsychologistId == psyId && a.Status == "Active")
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
            return RedirectToAction(nameof(ReviewWorksheet), new { id = w.WorksheetId });
        }

        // ═════════════════════════════════════════
        //  Bridge: Settings — AcceptsB2B toggle (visibility in HR directory)
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            return View(new LightenUp.Web.Models.ViewModels.PsySettingsViewModel { AcceptsB2B = psy.AcceptsB2B });
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
            TempData["success"] = "Pengaturan disimpan.";
            return RedirectToAction(nameof(Settings));
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
}