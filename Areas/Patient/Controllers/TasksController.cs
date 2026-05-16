using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<LightenUp.Web.Models.Patient?> GetPatientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ═════════════════════════════════════════════════════════════════
        //  List
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? search, List<string>? status, List<string>? period, int page = 1)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var q = _context.Worksheets
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .Where(w => w.PatientId == patient.PatientId);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(w => w.TaskName.Contains(search) || (w.Description ?? "").Contains(search));

            // Status filter (multi-select)
            if (status != null && status.Count > 0)
                q = q.Where(w => status.Contains(w.Status));

            // Period filter (multi-select, OR within group). All operate on CreatedAt per spec.
            if (period != null && period.Count > 0)
            {
                var today = DateTime.Today;
                var cutoffs = new List<DateTime>();
                if (period.Contains("HariIni")) cutoffs.Add(today);
                if (period.Contains("Mingguan")) cutoffs.Add(today.AddDays(-7));
                if (period.Contains("Bulanan")) cutoffs.Add(today.AddDays(-30));
                if (period.Contains("Tahunan")) cutoffs.Add(today.AddDays(-365));
                if (cutoffs.Count > 0)
                {
                    var earliest = cutoffs.Min();    // OR semantics → use the widest window
                    q = q.Where(w => w.CreatedAt >= earliest);
                }
            }

            int pageSize = 5;
            int total = await q.CountAsync();

            var rows = await q
                .OrderByDescending(w => w.CreatedAt)
                .Take(page * pageSize)
                .ToListAsync();

            var items = rows.Select(w => new TaskListItemViewModel
            {
                WorksheetId = w.WorksheetId,
                TaskName = w.TaskName,
                Description = w.Description,
                Status = w.Status,
                StatusLabel = WorksheetStatus.Label(w.Status),
                PsychologistName = w.Psychologist?.User?.FullName ?? "Dr. ...",
                DateLabel = ComputeDateLabel(w),
                Deadline = w.Deadline,
                ReviewedAt = w.ReviewedAt
            }).ToList();

            var vm = new TaskListViewModel
            {
                Search = search,
                Statuses = status ?? new(),
                Periods = period ?? new(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Detail / Submit
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var w = await _context.Worksheets
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(w => w.WorksheetId == id && w.PatientId == patient.PatientId);
            if (w == null) return NotFound();

            ViewBag.ActiveNav = "Beranda";
            return View(new TaskDetailViewModel
            {
                WorksheetId = w.WorksheetId,
                TaskName = w.TaskName,
                Description = w.Description,
                Deadline = w.Deadline,
                Status = w.Status,
                StatusLabel = WorksheetStatus.Label(w.Status),
                PsychologistName = w.Psychologist?.User?.FullName ?? "Dr. ...",
                ProofImagePath = w.ProofImagePath,
                Note = w.Note,
                PsychologistFeedback = w.PsychologistFeedback
            });
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Submit(TaskSubmitViewModel model)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var w = await _context.Worksheets.FirstOrDefaultAsync(x => x.WorksheetId == model.WorksheetId && x.PatientId == patient.PatientId);
            if (w == null) return NotFound();

            // Locked once psychologist has marked Completed.
            if (w.Status == WorksheetStatus.Completed)
            {
                TempData["error"] = "Tugas ini sudah ditinjau dan tidak dapat diubah.";
                return RedirectToAction(nameof(Detail), new { id = w.WorksheetId });
            }

            // Save photo if uploaded
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (!allowed.Contains(ext))
                {
                    TempData["error"] = "Format foto harus JPG, PNG, WEBP, atau GIF.";
                    return RedirectToAction(nameof(Detail), new { id = w.WorksheetId });
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "worksheets", patient.PatientId.ToString());
                Directory.CreateDirectory(folder);
                var fileName = $"{w.WorksheetId}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }
                w.ProofImagePath = $"/uploads/worksheets/{patient.PatientId}/{fileName}";
            }

            // Save note
            w.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;

            // Status transitions
            if (w.Status == WorksheetStatus.Assigned)
            {
                w.Status = WorksheetStatus.InProgress;
                w.SubmittedAt = DateTime.Now;
            }
            else if (w.Status == WorksheetStatus.InProgress)
            {
                // Editing an existing submission — update SubmittedAt to reflect latest edit
                w.SubmittedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Submitted), new { id = w.WorksheetId });
        }

        [HttpGet]
        public async Task<IActionResult> Submitted(int id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var exists = await _context.Worksheets.AnyAsync(w => w.WorksheetId == id && w.PatientId == patient.PatientId);
            if (!exists) return NotFound();
            ViewBag.ActiveNav = "Beranda";
            return View(model: id);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Helpers
        // ═════════════════════════════════════════════════════════════════
        private static string ComputeDateLabel(Worksheet w)
        {
            if (w.Status == WorksheetStatus.Completed && w.ReviewedAt.HasValue)
            {
                // "19 Oktober 2025"
                return w.ReviewedAt.Value.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
            }

            var days = (int)(w.Deadline.Date - DateTime.Today).TotalDays;
            if (days < 0) return $"{-days} hari lewat";
            if (days == 0) return "Hari ini";
            if (days < 7) return $"{days} hari";
            if (days < 30) return $"{days / 7} minggu";
            if (days < 365) return $"{days / 30} bulan";
            return $"{days / 365} tahun";
        }
    }
}
