using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    // #Class TasksController#
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserUploadService _uploads;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, UserUploadService uploads)
        {
            _context = context;
            _userManager = userManager;
            _uploads = uploads;
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
        // #Function Index#
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

            // Period filter (multi-select, OR within group). Operates on Deadline.
            if (period != null && period.Count > 0)
            {
                var today = DateTime.Today;
                var minDate = DateTime.MaxValue;
                var maxDate = DateTime.MinValue;

                if (period.Contains("HariIni")) 
                {
                    if (today < minDate) minDate = today;
                    if (today > maxDate) maxDate = today;
                }
                if (period.Contains("Mingguan")) 
                {
                    if (today.AddDays(-7) < minDate) minDate = today.AddDays(-7);
                    if (today.AddDays(7) > maxDate) maxDate = today.AddDays(7);
                }
                if (period.Contains("Bulanan")) 
                {
                    if (today.AddDays(-30) < minDate) minDate = today.AddDays(-30);
                    if (today.AddDays(30) > maxDate) maxDate = today.AddDays(30);
                }
                if (period.Contains("Tahunan")) 
                {
                    if (today.AddDays(-365) < minDate) minDate = today.AddDays(-365);
                    if (today.AddDays(365) > maxDate) maxDate = today.AddDays(365);
                }
                
                if (minDate != DateTime.MaxValue && maxDate != DateTime.MinValue)
                {
                    q = q.Where(w => w.Deadline.Date >= minDate && w.Deadline.Date <= maxDate);
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
            ViewBag.ActiveNav = "Tugas";

            // Active psychologist info for hero
            var activeAssignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            ViewBag.HasActivePsychologist = activeAssignment != null;
            ViewBag.PsychologistName = activeAssignment?.Psychologist?.User?.FullName;

            bool taskIsB2B = patient.CompanyId != null;
            bool taskIsMitra = !taskIsB2B && patient.SponsorType == "Psychologist" && patient.SponsorPsychologistId != null;
            ViewBag.IsB2B = taskIsB2B;

            if (activeAssignment != null)
            {
                if (taskIsB2B)
                {
                    var companySub = await _context.CompanySubscriptions
                        .Where(s => s.CompanyId == patient.CompanyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefaultAsync();
                    ViewBag.SubscriptionEndDate = companySub?.EndDate;
                    ViewBag.IsSubscriptionExpired = companySub == null;
                }
                else if (taskIsMitra)
                {
                    var mitraSub = await _context.PsychologistSubscriptions
                        .Where(s => s.PsychologistId == patient.SponsorPsychologistId
                            && s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .FirstOrDefaultAsync();

                    if (mitraSub == null)
                    {
                        // Lazy cancel
                        activeAssignment.Status = "Cancelled";
                        activeAssignment.CancellationReason = "Mitra subscription expired";
                        activeAssignment.CancellationRequestedAt = DateTime.UtcNow;
                        patient.SponsorType = "Self";
                        patient.SponsorPsychologistId = null;
                        await _context.SaveChangesAsync();

                        ViewBag.HasActivePsychologist = false;
                        ViewBag.PsychologistName = null;
                        ViewBag.IsSubscriptionExpired = false;
                    }
                    else
                    {
                        ViewBag.SubscriptionEndDate = mitraSub.EndDate;
                        ViewBag.IsSubscriptionExpired = false;
                    }
                }
                else
                {
                    var activeSub = await _context.Subscriptions
                        .Where(s => s.PatientId == patient.PatientId
                            && s.PsychologistId == activeAssignment.PsychologistId
                            && s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefaultAsync();
                    ViewBag.SubscriptionEndDate = activeSub?.EndDate;
                    ViewBag.IsSubscriptionExpired = activeSub == null;
                }
            }

            return View(vm);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Detail / Submit
        // ═════════════════════════════════════════════════════════════════
        // #Function Detail#
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var w = await _context.Worksheets
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(w => w.WorksheetId == id && w.PatientId == patient.PatientId);
            if (w == null) return NotFound();

            ViewBag.ActiveNav = "Tugas";
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

        // #Function DetailModal#

        [HttpGet]
        public async Task<IActionResult> DetailModal(int id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return Unauthorized();

            var w = await _context.Worksheets
                .Include(w => w.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(w => w.WorksheetId == id && w.PatientId == patient.PatientId);
            if (w == null) return NotFound();

            return PartialView("_DetailModal", new TaskDetailViewModel
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
        // #Function Submit#
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
                var path = await _uploads.ReplaceAsync(
                    patient.UserId, UserUploadService.Categories.Worksheets, model.Photo,
                    w.ProofImagePath, namePrefix: $"ws{w.WorksheetId}",
                    allowedExtensions: UserUploadService.ImageExtensions);
                if (path == null)
                {
                    TempData["error"] = "Format foto harus JPG, PNG, WEBP, atau GIF.";
                    return RedirectToAction(nameof(Detail), new { id = w.WorksheetId });
                }
                w.ProofImagePath = path;
            }

            // Save note
            w.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;

            // Status transitions
            if (w.Status == WorksheetStatus.Assigned)
            {
                w.Status = WorksheetStatus.InProgress;
                w.SubmittedAt = DateTime.UtcNow;
            }
            else if (w.Status == WorksheetStatus.InProgress || w.Status == WorksheetStatus.NeedsRevision)
            {
                // Editing an existing submission or resubmitting revision
                w.Status = WorksheetStatus.InProgress;
                w.SubmittedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Data Berhasil Disimpan!";
            return RedirectToAction(nameof(Index));
        }

        // #Function Submitted#

        [HttpGet]
        public async Task<IActionResult> Submitted(int id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var exists = await _context.Worksheets.AnyAsync(w => w.WorksheetId == id && w.PatientId == patient.PatientId);
            if (!exists) return NotFound();
            ViewBag.ActiveNav = "Tugas";
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
