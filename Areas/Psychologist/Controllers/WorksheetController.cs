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
    public class WorksheetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorksheetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        private static (string Label, string Css) MapStatus(string dbStatus) => dbStatus switch
        {
            "Assigned"   => ("Belum Dikerjakan", "belum"),
            "InProgress" => ("Review",            "review"),
            "Completed"  => ("Selesai",           "selesai"),
            _            => (dbStatus,             "")
        };

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
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["success"] = "Worksheet baru ditambahkan.";

            if (model.ReturnPatientId.HasValue)
                return RedirectToAction(nameof(PatientWorksheetHistory), new { id = model.ReturnPatientId.Value });
            return RedirectToAction(nameof(Worksheet));
        }

        [HttpGet]
        public async Task<IActionResult> EditWorksheet(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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

        [HttpGet]
        public async Task<IActionResult> Worksheet(bool add = false, int? patientId = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

            var rows = await _context.Worksheets
                .Include(w => w.Patient).ThenInclude(p => p!.User)
                .Where(w => w.PsychologistId == psyId)
                .OrderByDescending(w => w.CreatedAt)
                .Take(50)
                .ToListAsync();

            var tasks = rows.Select(w =>
            {
                var (label, css) = MapStatus(w.Status);
                return new LightenUp.Web.Models.ViewModels.WorksheetItemViewModel
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

            return View("Worksheet", new LightenUp.Web.Models.ViewModels.WorksheetViewModel
            {
                TotalActivities = rows.Count,
                Tasks = tasks
            });
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
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
        public async Task<IActionResult> PatientWorksheetHistory(int id, bool add = false, int? open = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
                openWorksheetId: open,
                patientName: patient.User?.FullName ?? "—",
                worksheets: worksheets);
        }

        private async Task<IActionResult> PatientWorksheetHistoryViewAsync(
            int patientId,
            LightenUp.Web.Models.ViewModels.PsyAddTaskViewModel? addForm = null,
            bool openModal = false,
            int? openWorksheetId = null,
            string? patientName = null,
            List<Worksheet>? worksheets = null)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Index", "Dashboard");

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
            ViewBag.OpenWorksheetId = openWorksheetId;
            return View("PatientWorksheetHistory");
        }

        [HttpGet]
        public async Task<IActionResult> ReviewWorksheet(int id)
        {
            var psyId = await CurrentPsychologistIdAsync();
            if (psyId == null) return RedirectToAction("Login", "Account", new { area = "" });

            var w = await _context.Worksheets
                .FirstOrDefaultAsync(x => x.WorksheetId == id && x.PsychologistId == psyId);
            if (w == null) return NotFound();

            return RedirectToAction(nameof(PatientWorksheetHistory), new { id = w.PatientId, open = w.WorksheetId });
        }

        [HttpPost]
        public async Task<IActionResult> ReviewWorksheet(LightenUp.Web.Models.ViewModels.PsyWorksheetReviewViewModel model, string submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (psy == null) return NotFound();

            var w = await _context.Worksheets
                .FirstOrDefaultAsync(x => x.WorksheetId == model.WorksheetId && x.PsychologistId == psy.PsychologistId);
            if (w == null) return NotFound();

            w.PsychologistFeedback = string.IsNullOrWhiteSpace(model.PsychologistFeedback) ? null : model.PsychologistFeedback;

            if (submitAction == "Complete")
            {
                w.Status = "Completed";
                w.ReviewedAt = DateTime.UtcNow;
                TempData["success"] = "Worksheet diselesaikan.";
            }
            else if (submitAction == "Revision")
            {
                w.Status = "NeedsRevision";
                w.ReviewedAt = null;
                TempData["success"] = "Worksheet dikembalikan ke pasien untuk direvisi.";
            }
            else
            {
                TempData["success"] = "Catatan disimpan.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Worksheet));
        }
    }
}
