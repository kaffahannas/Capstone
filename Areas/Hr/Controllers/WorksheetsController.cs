using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class WorksheetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorksheetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs.FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        // ═════════════════════════════════════════════
        //  List
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? search, List<string>? status, string? period, int page = 1)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");
            var companyId = hr.CompanyId.Value;

            ViewBag.ModalPatients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active")
                .OrderBy(p => p.User!.FullName)
                .Select(p => new HrSimplePatient { PatientId = p.PatientId, FullName = p.User!.FullName, Department = p.Department })
                .ToListAsync();

            var q = _context.Worksheets
                .Include(w => w.Patient).ThenInclude(p => p!.User)
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .Where(w => w.Patient!.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(w => w.Patient!.User!.FullName.Contains(search) || w.TaskName.Contains(search));
            if (status != null && status.Count > 0)
                q = q.Where(w => status.Contains(w.Status));
            if (!string.IsNullOrEmpty(period))
            {
                var today = DateTime.Today;
                DateTime cutoff = period switch
                {
                    "HariIni" => today,
                    "Mingguan" => today.AddDays(-7),
                    "Bulanan" => today.AddDays(-30),
                    "Tahunan" => today.AddDays(-365),
                    _ => DateTime.MinValue
                };
                if (cutoff != DateTime.MinValue) q = q.Where(w => w.CreatedAt >= cutoff);
            }

            int pageSize = 10;
            int total = await q.CountAsync();
            var rows = await q
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rows.Select(w => new HrWorksheetListItem
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Status = w.Status,
                StatusLabel = WorksheetStatus.Label(w.Status),
                CreatedAt = w.CreatedAt,
                Deadline = w.Deadline,
                DateLabel = w.ReviewedAt?.ToString("d MMM yyyy", new System.Globalization.CultureInfo("id-ID"))
                            ?? w.Deadline.ToString("d MMM yyyy", new System.Globalization.CultureInfo("id-ID")),
                PsychologistName = w.Psychologist?.User?.FullName ?? "—"
            }).ToList();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrWorksheetListViewModel
            {
                Search = search,
                Statuses = status ?? new(),
                Period = period,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            });
        }

        // ═════════════════════════════════════════════
        //  Review (read most data, edit HrNote)
        // ═════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var w = await _context.Worksheets
                .Include(x => x.Patient).ThenInclude(p => p!.User)
                .Include(x => x.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.Patient!.CompanyId == hr.CompanyId);
            if (w == null) return NotFound();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrWorksheetReviewViewModel
            {
                WorksheetId = w.WorksheetId,
                PatientName = w.Patient?.User?.FullName ?? "—",
                PatientEmail = w.Patient?.User?.Email,
                PsychologistEmail = w.Psychologist?.User?.Email,
                PsychologistName = w.Psychologist?.User?.FullName ?? "—",
                TaskName = w.TaskName,
                Description = w.Description,
                Status = w.Status,
                StatusLabel = WorksheetStatus.Label(w.Status),
                SubmittedAt = w.SubmittedAt,
                ProofImagePath = w.ProofImagePath,
                PatientNote = w.Note,
                PsychologistFeedback = w.PsychologistFeedback,
                HrNote = w.HrNote
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveNote(HrWorksheetEditNoteViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return Unauthorized();
            var w = await _context.Worksheets
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.WorksheetId == model.WorksheetId && x.Patient!.CompanyId == hr.CompanyId);
            if (w == null) return NotFound();

            w.HrNote = string.IsNullOrWhiteSpace(model.HrNote) ? null : model.HrNote;
            await _context.SaveChangesAsync();
            TempData["success"] = "Catatan disimpan.";
            return RedirectToAction(nameof(Review), new { id = model.WorksheetId });
        }

        // ═════════════════════════════════════════════
        //  RequestModal  (AJAX — used by Index modal)
        // ═════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestModal(HrRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { ok = false, errors });
            }

            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null)
                return Json(new { ok = false, errors = new[] { "Sesi tidak valid. Silakan login ulang." } });

            var psychologistId = await _context.Assignments
                .Where(a => a.PatientId == model.PatientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => (int?)a.PsychologistId)
                .FirstOrDefaultAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                RequestedByHrUserId = user!.Id,
                PatientId = model.PatientId,
                PsychologistId = psychologistId,
                RequestType = "Worksheet",
                Notes = model.Notes,
                ProposedTaskName = model.ProposedTaskName,
                ProposedDeadline = model.ProposedDeadline,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }

        // ═════════════════════════════════════════════
        //  Request (HR asks psychologist to assign a new worksheet)
        // ═════════════════════════════════════════════
        [HttpGet]
        public new async Task<IActionResult> Request()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == hr.CompanyId && p.EmploymentStatus == "active")
                .OrderBy(p => p.User!.FullName)
                .Select(p => new HrSimplePatient
                {
                    PatientId = p.PatientId,
                    FullName = p.User!.FullName,
                    Department = p.Department
                })
                .ToListAsync();

            ViewBag.ActiveNav = "Monitoring";
            return View(new HrRequestViewModel
            {
                RequestType = "Worksheet",
                AvailablePatients = patients
            });
        }

        [HttpPost]
        public new async Task<IActionResult> Request(HrRequestViewModel model)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            // Resolve patient's active psychologist
            var psychologistId = await _context.Assignments
                .Where(a => a.PatientId == model.PatientId && a.Status == "Active")
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => (int?)a.PsychologistId)
                .FirstOrDefaultAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Pastikan Anda telah mengisi formulir dengan benar.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                RequestedByHrUserId = user!.Id,
                PatientId = model.PatientId,
                PsychologistId = psychologistId,
                RequestType = "Worksheet",
                Notes = model.Notes,
                ProposedTaskName = model.ProposedTaskName,
                ProposedDeadline = model.ProposedDeadline,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan worksheet dikirim ke psikolog terkait.";
            return RedirectToAction(nameof(Index));
        }
    }
}
