using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs.Include(h => h.Company).FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        private static double FeelingScore(string feeling) => feeling switch
        {
            "Overjoyed" => 5, "Happy" => 4, "Calm" => 4,
            "Neutral" => 3, "Disappointed" => 2, "Angry" => 1, _ => 0
        };

        // ═════════════════════════════════════════
        //  Index — Draft + Sent tabs
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "All")
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Welcome", "Onboarding");
            var user = await _userManager.GetUserAsync(User);

            var q = _context.Reports
                .Include(r => r.Patient).ThenInclude(p => p!.User)
                .Include(r => r.Psychologist).ThenInclude(p => p!.User)
                .Where(r => r.ReportedByHrUserId == user!.Id);

            ViewBag.AllCount = await q.CountAsync();
            ViewBag.DraftCount = await q.CountAsync(r => r.Status == "Draft");
            ViewBag.SentCount = await q.CountAsync(r => r.Status == "Sent");

            if (tab == "Draft") q = q.Where(r => r.Status == "Draft");
            else if (tab == "Sent") q = q.Where(r => r.Status == "Sent");

            var items = await q.OrderByDescending(r => r.CreatedAt).Select(r => new HrReportListItem
            {
                ReportId = r.Id,
                PatientName = r.Patient!.User!.FullName,
                PsychologistName = r.Psychologist!.User!.FullName,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                EmailSentAt = r.EmailSentAt
            }).ToListAsync();

            ViewBag.ActiveNav = "Laporan";
            return View(new HrReportListViewModel { Tab = tab, Items = items });
        }

        // ═════════════════════════════════════════
        //  Create — POPULATED from patient data
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Create(int patientId)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var patient = await _context.Patients.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && p.CompanyId == hr.CompanyId);
            if (patient == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .Where(a => a.PatientId == patientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();
            if (assignment?.Psychologist == null)
            {
                TempData["error"] = "Pasien belum memiliki psikolog aktif. Tetapkan psikolog terlebih dahulu.";
                return RedirectToAction("Detail", "Employees", new { id = patientId });
            }

            var vm = await BuildCreateViewModel(patient, assignment.Psychologist, hr, notes: null);
            ViewBag.ActiveNav = "Laporan";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null) return RedirectToAction("Welcome", "Onboarding");
            var user = await _userManager.GetUserAsync(User);

            var report = await _context.Reports
                .Include(r => r.Patient).ThenInclude(p => p!.User)
                .Include(r => r.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.ReportedByHrUserId == user!.Id && r.Status == "Draft");
            if (report == null) return NotFound();

            var vm = await BuildCreateViewModel(report.Patient!, report.Psychologist!, hr, report.Notes);
            vm.ReportId = report.Id;
            ViewBag.ActiveNav = "Laporan";
            return View("Create", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditModal(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null) return Unauthorized();
            var user = await _userManager.GetUserAsync(User);

            var report = await _context.Reports
                .Include(r => r.Patient).ThenInclude(p => p!.User)
                .Include(r => r.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.ReportedByHrUserId == user!.Id);
            if (report == null) return NotFound();

            var vm = await BuildCreateViewModel(report.Patient!, report.Psychologist!, hr, report.Notes);
            vm.ReportId = report.Id;
            ViewBag.Status = report.Status;
            return PartialView("_EditModal", vm);
        }

        // ═════════════════════════════════════════
        //  Save as Draft / Send
        // ═════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Save(HrReportCreateViewModel model) =>
            await UpsertAndOptionallySend(model, send: false);

        [HttpPost]
        public async Task<IActionResult> Send(HrReportCreateViewModel model) =>
            await UpsertAndOptionallySend(model, send: true);

        private async Task<IActionResult> UpsertAndOptionallySend(HrReportCreateViewModel model, bool send)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var user = await _userManager.GetUserAsync(User);

            var patient = await _context.Patients.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == model.PatientId && p.CompanyId == hr.CompanyId);
            if (patient == null) return NotFound();
            var psy = await _context.Psychologists.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PsychologistId == model.PsychologistId);
            if (psy == null) return NotFound();

            // Recompute metrics so snapshot is fresh at save time
            var fresh = await BuildCreateViewModel(patient, psy, hr, model.Notes);

            Report report;
            if (model.ReportId.HasValue)
            {
                report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == model.ReportId.Value
                    && r.ReportedByHrUserId == user!.Id) ?? throw new InvalidOperationException();
            }
            else
            {
                report = new Report
                {
                    ReportedByHrUserId = user!.Id,
                    PatientId = patient.PatientId,
                    PsychologistId = psy.PsychologistId,
                    CreatedAt = DateTime.Now
                };
                _context.Reports.Add(report);
            }

            report.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes;
            report.EmailSubject = fresh.PreviewEmailSubject;
            report.EmailBody = fresh.PreviewEmailBody;

            if (send)
            {
                if (string.IsNullOrEmpty(psy.User?.Email))
                {
                    TempData["error"] = "Psikolog belum memiliki email terdaftar.";
                    return RedirectToAction(nameof(Index));
                }
                try
                {
                    await _emailSender.SendAsync(psy.User.Email, fresh.PreviewEmailSubject, fresh.PreviewEmailBody);
                    report.Status = "Sent";
                    report.EmailSentAt = DateTime.Now;
                    TempData["success"] = $"Laporan terkirim ke {psy.User.FullName}.";
                }
                catch (InvalidOperationException ex)
                {
                    // SMTP not configured — save as draft, surface error
                    report.Status = "Draft";
                    await _context.SaveChangesAsync();
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    report.Status = "Draft";
                    await _context.SaveChangesAsync();
                    TempData["error"] = $"Pengiriman email gagal: {ex.Message}";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                report.Status = "Draft";
                TempData["success"] = "Laporan disimpan sebagai draft.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { tab = send ? "Sent" : "Draft" });
        }

        // ═════════════════════════════════════════
        //  CreateModal  (AJAX — used by Employee Detail modal)
        // ═════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModal(
            [FromForm] int patientId,
            [FromForm] string? notes,
            [FromForm] string? send)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null)
                return Json(new { ok = false, errors = new[] { "Sesi tidak valid. Silakan login ulang." } });

            var patient = await _context.Patients.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && p.CompanyId == hr.CompanyId);
            if (patient == null)
                return Json(new { ok = false, errors = new[] { "Karyawan tidak ditemukan." } });

            var assignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .Where(a => a.PatientId == patientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .FirstOrDefaultAsync();

            if (assignment?.Psychologist == null)
                return Json(new { ok = false, errors = new[] { "Karyawan belum memiliki psikolog aktif. Tetapkan psikolog terlebih dahulu." } });

            var user = await _userManager.GetUserAsync(User);
            var vm = await BuildCreateViewModel(patient, assignment.Psychologist, hr, notes);

            var report = new Report
            {
                ReportedByHrUserId = user!.Id,
                PatientId = patient.PatientId,
                PsychologistId = assignment.Psychologist.PsychologistId,
                CreatedAt = DateTime.Now,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                EmailSubject = vm.PreviewEmailSubject,
                EmailBody = vm.PreviewEmailBody
            };

            bool doSend = send == "true";
            if (doSend)
            {
                var psyEmail = assignment.Psychologist.User?.Email;
                if (string.IsNullOrEmpty(psyEmail))
                    return Json(new { ok = false, errors = new[] { "Psikolog belum memiliki email terdaftar." } });

                try
                {
                    await _emailSender.SendAsync(psyEmail, vm.PreviewEmailSubject, vm.PreviewEmailBody);
                    report.Status = "Sent";
                    report.EmailSentAt = DateTime.Now;
                }
                catch (Exception ex)
                {
                    report.Status = "Draft";
                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();
                    return Json(new { ok = false, errors = new[] { $"Pengiriman email gagal: {ex.Message}. Laporan disimpan sebagai draft." } });
                }
            }
            else
            {
                report.Status = "Draft";
            }

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var msg = doSend
                ? $"Laporan terkirim ke {assignment.Psychologist.User?.FullName ?? "psikolog"}."
                : "Laporan disimpan sebagai draft.";
            return Json(new { ok = true, message = msg });
        }

        // ═════════════════════════════════════════
        //  Helpers
        // ═════════════════════════════════════════
        private async Task<HrReportCreateViewModel> BuildCreateViewModel(LightenUp.Web.Models.Patient patient,
            Psychologist psy, HrStaff hr, string? notes)
        {
            var today = DateTime.Today;
            var from7 = today.AddDays(-6);

            var moods = await _context.MoodTrackers
                .Where(m => m.PatientId == patient.PatientId && m.MoodDate >= from7)
                .OrderBy(m => m.MoodDate)
                .ToListAsync();

            var trendDates = Enumerable.Range(0, 7).Select(i => from7.AddDays(i)).ToList();
            var trendScores = trendDates.Select(d =>
            {
                var m = moods.FirstOrDefault(x => x.MoodDate.Date == d.Date);
                return m == null ? 0.0 : FeelingScore(m.Feeling);
            }).ToList();

            // Assignment score = avg OverallScore over last 7 days × 2 (→ 0-10 scale)
            var checkIns = await _context.JournalCheckIns
                .Where(c => c.PatientId == patient.PatientId && c.CheckInDate >= from7)
                .ToListAsync();
            double assignScore = 0;
            if (checkIns.Count > 0)
                assignScore = Math.Round(checkIns.Average(c => c.OverallScore) * 2.0, 1);

            // Trend comparison: last 7 vs prior 7
            var from14 = today.AddDays(-13);
            var prevCheckIns = await _context.JournalCheckIns
                .Where(c => c.PatientId == patient.PatientId && c.CheckInDate >= from14 && c.CheckInDate < from7)
                .ToListAsync();
            string trend = "Stabil";
            if (checkIns.Count > 0 && prevCheckIns.Count > 0)
            {
                var a = checkIns.Average(c => c.OverallScore);
                var b = prevCheckIns.Average(c => c.OverallScore);
                if (a < b - 0.3) trend = "Menurun";
                else if (a > b + 0.3) trend = "Meningkat";
            }

            string stress = patient.MentalHealthStatus switch
            {
                "Sehat" => "Rendah",
                "Beresiko" => "Sedang",
                "Bahaya" => "Tinggi",
                _ => "Tidak diketahui"
            };

            var subject = $"[LightenUp] Laporan untuk {patient.User?.FullName ?? "Pasien"}";
            var sb = new StringBuilder();
            sb.AppendLine($"Halo Dr. {psy.User?.FullName ?? "Psikolog"},");
            sb.AppendLine();
            sb.AppendLine($"Laporan dari {(await _userManager.GetUserAsync(User))?.FullName ?? "HR"} ({hr.Company?.Name}) ");
            sb.AppendLine($"mengenai karyawan {patient.User?.FullName} ({patient.Department ?? "—"}) ");
            sb.AppendLine($"per {DateTime.Now:d MMMM yyyy}.");
            sb.AppendLine();
            sb.AppendLine($"• Status mental: {patient.MentalHealthStatus}");
            sb.AppendLine($"• Tingkat stress: {stress}");
            sb.AppendLine($"• Nilai penugasan: {assignScore}/10 ({trend})");
            sb.AppendLine($"• Mood 7 hari terakhir (skala 1-5): {string.Join(", ", trendScores)}");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(notes))
            {
                sb.AppendLine("Catatan dari HR:");
                sb.AppendLine(notes);
                sb.AppendLine();
            }
            sb.AppendLine("Login ke LightenUp untuk meninjau detail dan menanggapi.");

            return new HrReportCreateViewModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.User?.FullName ?? "—",
                PatientDepartment = patient.Department,
                PatientStatus = patient.MentalHealthStatus,
                PsychologistId = psy.PsychologistId,
                PsychologistName = psy.User?.FullName ?? "—",
                PsychologistEmail = psy.User?.Email,
                MoodTrendDates = trendDates,
                MoodTrendScores = trendScores,
                MoodTrendLabel = trend,
                AssignmentScore = assignScore,
                AssignmentTrend = trend,
                StressLevel = stress,
                Notes = notes,
                PreviewEmailSubject = subject,
                PreviewEmailBody = sb.ToString()
            };
        }
    }
}
