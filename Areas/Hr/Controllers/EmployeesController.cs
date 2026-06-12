using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionAccessService _access;

        public EmployeesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _access = access;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs.Include(h => h.Company)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        // ═════════════════════════════════════════════
        //  List
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "Semua", string? search = null,
            string? division = null, string? status = null, int page = 1)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;
            var today = DateTime.Today;

            var q = _context.Patients
                .Include(p => p.User)
                .Include(p => p.Division)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active");

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => p.User!.FullName.Contains(search));
            if (!string.IsNullOrWhiteSpace(division))
                q = q.Where(p => p.Division != null && p.Division.Name == division);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(p => p.MentalHealthStatus == status);

            if (tab == "Sesi")
            {
                // Patients with a Scheduled session today
                q = q.Where(p => _context.Schedules.Any(s =>
                    s.PatientId == p.PatientId &&
                    s.Status == "Scheduled" &&
                    s.SessionStart >= today &&
                    s.SessionStart < today.AddDays(1)));
            }

            int pageSize = 10;
            int total = await q.CountAsync();
            var rows = await q
                .OrderBy(p => p.User!.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<HrEmployeeListItem>();
            foreach (var p in rows)
            {
                var item = new HrEmployeeListItem
                {
                    PatientId = p.PatientId,
                    FullName = p.User?.FullName ?? "—",
                    Gender = p.Gender,
                    Department = p.Division != null ? p.Division.Name : "Belum Diatur",
                    ProfilePicture = p.User?.ProfilePicture,
                    Status = p.MentalHealthStatus,
                    OnboardingCompletedAt = p.OnboardingCompletedAt
                };

                if (tab == "Sesi")
                {
                    var todaySession = await _context.Schedules
                        .Where(s => s.PatientId == p.PatientId && s.Status == "Scheduled"
                             && s.SessionStart >= today && s.SessionStart < today.AddDays(1))
                        .OrderBy(s => s.SessionStart)
                        .FirstOrDefaultAsync();
                    if (todaySession != null)
                    {
                        item.TodaySessionStart = todaySession.SessionStart;
                        item.TodaySessionDurationMinutes = todaySession.DurationMinutes;
                        item.TodaySessionTitle = string.IsNullOrEmpty(todaySession.Notes) ? "General Counseling" : todaySession.Notes;
                    }
                }
                items.Add(item);
            }

            var divisions = await _context.Patients
                .Include(p => p.Division)
                .Where(p => p.CompanyId == companyId && p.Division != null)
                .Select(p => p.Division!.Name)
                .Distinct().OrderBy(d => d)
                .ToListAsync();

            ViewBag.ActiveNav = "Klien";
            return View(new HrEmployeesListViewModel
            {
                Tab = tab,
                Search = search,
                Division = division,
                Status = status,
                Divisions = divisions,
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }

        // ═════════════════════════════════════════════
        //  Detail
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id, string window = "Week")
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var p = await _context.Patients
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.PatientId == id && x.CompanyId == companyId);
            if (p == null) return NotFound();

            string ageStr = "—";
            if (p.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                int age = today.Year - p.DateOfBirth.Value.Year;
                if (p.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                ageStr = $"{age} tahun";
            }

            var todayJournal = await _context.Journals
                .Where(j => j.PatientId == id && j.JournalDate.Date == DateTime.Today)
                .FirstOrDefaultAsync();

            // ─── Chart window ───
            int days = window == "Month" ? 30 : 7;
            var from = DateTime.Today.AddDays(-days + 1);
            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == id && m.MoodDate >= from)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            // Build dates list (every day in window, with score 0 where missing)
            var chartDates = Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList();
            var chartScores = chartDates.Select(d =>
            {
                var m = moods.FirstOrDefault(x => x.MoodDate.Date == d.Date);
                return m == null ? 0.0 : FeelingScore(m.Feeling);
            }).ToList();

            // Status %
            int sehat = 0, beresiko = 0, bahaya = 0;
            foreach (var m in moods)
            {
                var s = FeelingScore(m.Feeling);
                if (s >= 4) sehat++;
                else if (s >= 2.5) beresiko++;
                else bahaya++;
            }
            int total = Math.Max(1, moods.Count);

            var nextSession = await _context.Schedules
                .Where(s => s.PatientId == id && s.Status == "Scheduled" && s.SessionStart > DateTime.UtcNow)
                .OrderBy(s => s.SessionStart)
                .FirstOrDefaultAsync();

            var openWorksheets = await _context.Worksheets
                .CountAsync(w => w.PatientId == id && w.Status != "Completed");

            var activeAssignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(psy => psy!.User)
                .FirstOrDefaultAsync(a => a.PatientId == id && (a.Status == "Active" || a.Status == "PendingCancellation" || a.Status == "PendingCancellationByAdmin"));

            ViewBag.ActiveNav = "Klien";
            return View(new HrEmployeeDetailViewModel
            {
                PatientId = p.PatientId,
                FullName = p.User?.FullName ?? "—",
                Gender = p.Gender == "Male" ? "Laki-laki" : (p.Gender == "Female" ? "Perempuan" : p.Gender),
                Age = ageStr,
                Address = p.Company?.Address,
                Phone = p.User?.PhoneNumber,
                ProfilePicture = p.User?.ProfilePicture,
                Status = p.MentalHealthStatus,
                Symptoms = p.Symptoms,
                TodayJournalSnippet = todayJournal?.Content,
                TodayJournalKeluhan = null,   // could be derived if model has keluhan; using catatan for now
                ChartWindow = window,
                ChartDates = chartDates,
                ChartScores = chartScores,
                SehatPct = (int)Math.Round((double)sehat / total * 100),
                BeresikoPct = (int)Math.Round((double)beresiko / total * 100),
                BahayaPct = (int)Math.Round((double)bahaya / total * 100),
                NextSessionStart = nextSession?.SessionStart,
                NextSessionNote = nextSession?.Notes,
                OpenWorksheetCount = openWorksheets,
                ActiveAssignmentId = activeAssignment?.AssignmentId,
                AssignedPsychologistName = activeAssignment?.Psychologist?.User?.FullName,
                AssignmentStatus = activeAssignment?.Status
            });
        }

        // ═════════════════════════════════════════════
        //  PDF per-employee (print-optimised, browser → Save as PDF)
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> EmployeePdf(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            var p = await _context.Patients
                .Include(x => x.User)
                .Include(x => x.Company)
                .FirstOrDefaultAsync(x => x.PatientId == id && x.CompanyId == companyId);
            if (p == null) return NotFound();

            // Mood last 30 days
            var from = DateTime.Today.AddDays(-29);
            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == id && m.MoodDate >= from)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            // Sessions
            var sessions = await _context.Schedules
                .Include(s => s.Psychologist).ThenInclude(ps => ps!.User)
                .Where(s => s.PatientId == id)
                .OrderByDescending(s => s.SessionStart)
                .Take(10)
                .ToListAsync();

            // Worksheets
            var worksheets = await _context.Worksheets
                .Where(w => w.PatientId == id)
                .OrderByDescending(w => w.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Patient = p;
            ViewBag.Moods = moods;
            ViewBag.Sessions = sessions;
            ViewBag.Worksheets = worksheets;
            ViewBag.CompanyName = hr.Company?.Name ?? "";
            ViewBag.GeneratedAt = DateTime.Now;

            return View();
        }

        // ═════════════════════════════════════════════
        //  Edit Symptoms (inline AJAX-friendly POST)
        // ═════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> EditSymptoms(HrEditSymptomsViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Unauthorized();
            var p = await _context.Patients.FirstOrDefaultAsync(x => x.PatientId == model.PatientId && x.CompanyId == hr.CompanyId);
            if (p == null) return NotFound();
            p.Symptoms = model.Symptoms;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = model.PatientId });
        }

        // ═════════════════════════════════════════════
        //  Add (PendingEmployee)
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            ViewBag.ActiveNav = "Klien";
            ViewBag.ReferralCode = hr.Company?.ReferralCode;
            ViewBag.Divisions = await _context.CompanyDivisions
                .Where(d => d.CompanyId == hr.CompanyId.Value)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(new HrAddClientViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Add(HrAddClientViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            ViewBag.ActiveNav = "Klien";
            ViewBag.ReferralCode = hr.Company?.ReferralCode;
            ViewBag.Divisions = await _context.CompanyDivisions
                .Where(d => d.CompanyId == companyId)
                .OrderBy(d => d.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var activeSub = await _access.GetActiveCompanySubscriptionAsync(companyId);
            if (activeSub?.EmployeeLimit > 0)
            {
                var currentCount = await _context.Patients.CountAsync(p =>
                    p.CompanyId == companyId && p.EmploymentStatus == "active");
                currentCount += await _context.PendingEmployees.CountAsync(p => p.CompanyId == companyId);
                if (currentCount >= activeSub.EmployeeLimit)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Kuota karyawan paket Anda sudah penuh ({activeSub.EmployeeLimit}). Upgrade paket langganan untuk menambah karyawan.");
                    ViewBag.ActiveNav = "Klien";
                    ViewBag.ReferralCode = hr.Company?.ReferralCode;
                    return View(model);
                }
            }

            // Uniqueness check: (CompanyId, Email) — model has UNIQUE index but we want a friendly error.
            var dup = await _context.PendingEmployees
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Email == model.Email.Trim());
            if (dup != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email ini sudah terdaftar sebagai client di perusahaan Anda.");
                ViewBag.ActiveNav = "Klien";
                ViewBag.ReferralCode = hr.Company?.ReferralCode;
                return View(model);
            }

            _context.PendingEmployees.Add(new PendingEmployee
            {
                CompanyId = companyId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                DivisionId = model.DivisionId,
                EmployeeId = model.EmployeeId?.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["success"] = $"{model.FullName} ditambahkan. Mereka dapat mendaftar dengan kode referral perusahaan.";
            return RedirectToAction(nameof(Index));
        }

        // ─── HR membatalkan psikolog karyawan (Request → PendingCancellationByAdmin) ───
        [HttpPost]
        public async Task<IActionResult> RequestCancelAssignment(int assignmentId, string? reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Forbid();

            var a = await _context.Assignments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId
                                       && a.Patient!.CompanyId == hr.CompanyId.Value
                                       && a.Status == "Active");
            if (a == null) return NotFound();

            a.Status = "PendingCancellationByAdmin";
            a.CancellationRequestedByUserId = user?.Id;
            a.CancellationReason = reason;
            a.CancellationRequestedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan pembatalan penugasan psikolog dikirim ke Admin untuk disetujui.";
            return RedirectToAction(nameof(Detail), new { id = a.PatientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmployeeRemoval(int PatientId, string Reason)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Json(new { ok = false, errors = new[] { "Sesi tidak valid." } });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == PatientId && p.CompanyId == hr.CompanyId.Value);
            if (patient == null) return Json(new { ok = false, errors = new[] { "Karyawan tidak ditemukan." } });

            var existing = await _context.HrEmployeeRemovalRequests.FirstOrDefaultAsync(r => r.PatientId == PatientId && r.Status == "Pending");
            if (existing != null) return Json(new { ok = false, errors = new[] { "Permintaan pemberhentian sudah diajukan dan sedang menunggu persetujuan Admin." } });

            var req = new HrEmployeeRemovalRequest
            {
                PatientId = patient.PatientId,
                RequestedByHrUserId = user?.Id,
                Reason = Reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.HrEmployeeRemovalRequests.Add(req);
            await _context.SaveChangesAsync();

            return Json(new { ok = true, message = "Permintaan pemberhentian karyawan berhasil dikirim ke Admin." });
        }

        // ─── Map feeling string → numeric ───
        private static double FeelingScore(string feeling) => feeling switch
        {
            "Overjoyed" => 5,
            "Happy" => 4,
            "Calm" => 4,
            "Neutral" => 3,
            "Disappointed" => 2,
            "Angry" => 1,
            _ => 0
        };
    }
}
