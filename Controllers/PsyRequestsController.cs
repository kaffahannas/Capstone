using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Controllers
{
    [Authorize(Roles = "Psychologist")]
    // #Class PsyRequestsController#
    public class PsyRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AssignmentActivationService _activation;

        public PsyRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AssignmentActivationService activation)
        {
            _context = context;
            _userManager = userManager;
            _activation = activation;
        }

        private async Task<Psychologist?> GetPsyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // #Bagian Inbox Permintaan#
        // #Function Index#
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "Pending")
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account");

            var q = _context.PsychologistRequests
                .Include(r => r.Patient).ThenInclude(p => p!.User)
                .Include(r => r.Patient).ThenInclude(p => p!.Company)
                .Include(r => r.RequestedByHr)
                .Include(r => r.RequestedByPatient)
                .Where(r => r.PsychologistId == psy.PsychologistId);

            if (tab is "Pending" or "Approved" or "Rejected")
                q = q.Where(r => r.Status == tab);

            var items = await q.OrderByDescending(r => r.CreatedAt).Select(r => new PsyRequestListItem
            {
                Id = r.Id,
                RequestType = r.RequestType,
                PatientName = r.Patient!.User!.FullName,
                CompanyName = r.Patient.Company != null ? r.Patient.Company.Name : "Publik (B2C)",
                HrName = r.RequestedByHr != null ? r.RequestedByHr.FullName : (r.RequestedByPatient != null ? r.RequestedByPatient.FullName : "—"),
                IsFromHr = r.RequestedByHr != null,
                Notes = r.Notes,
                ProposedTaskName = r.ProposedTaskName,
                ProposedDeadline = r.ProposedDeadline,
                ProposedSessionDate = r.ProposedSessionDate,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                RespondedNote = r.RespondedNote
            }).ToListAsync();

            if (tab is "Pending" or "All")
            {
                var assignmentItems = await _context.Assignments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .Include(a => a.Patient).ThenInclude(p => p!.Company)
                    .Include(a => a.RequestedBy)
                    .Where(a => a.PsychologistId == psy.PsychologistId && a.Status == "PendingPsychologistApproval")
                    .OrderByDescending(a => a.AssignedAt)
                    .Select(a => new PsyRequestListItem
                    {
                        Id = a.AssignmentId,
                        AssignmentId = a.AssignmentId,
                        RequestType = "Assignment",
                        PatientName = a.Patient!.User!.FullName,
                        CompanyName = a.Patient.Company != null ? a.Patient.Company.Name : "Publik (B2C)",
                        HrName = a.RequestedBy != null ? a.RequestedBy.FullName + " (Pasien)" : "Pasien",
                        Status = "Pending",
                        CreatedAt = a.AssignedAt
                    })
                    .ToListAsync();

                items = items.Concat(assignmentItems)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToList();
            }

            return View(new PsyRequestsViewModel { Tab = tab, Items = items });
        }

        // #Bagian Penugasan Pasien#
        // #Function AcceptAssignment#
        [HttpPost]
        public async Task<IActionResult> AcceptAssignment(int assignmentId)
        {
            var psy = await GetPsyAsync();
            if (psy == null) return Forbid();

            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId &&
                                         a.PsychologistId == psy.PsychologistId &&
                                         a.Status == "PendingPsychologistApproval");
            if (a == null) return NotFound();

            await _activation.ActivateAsync(a, user?.Id);
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pasien diterima. Penugasan kini aktif.";
            return RedirectToAction(nameof(Index));
        }

        // #Function RejectAssignment#
        [HttpPost]
        public async Task<IActionResult> RejectAssignment(int assignmentId, string? note)
        {
            var psy = await GetPsyAsync();
            if (psy == null) return Forbid();

            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId &&
                                         a.PsychologistId == psy.PsychologistId &&
                                         a.Status == "PendingPsychologistApproval");
            if (a == null) return NotFound();

            a.Status = "Rejected";
            a.DecisionByUserId = user?.Id;
            a.DecisionAt = DateTime.UtcNow;
            a.DecisionNote = note;
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pasien ditolak.";
            return RedirectToAction(nameof(Index));
        }

        // #Bagian Tanggapan Permintaan#
        // #Function Respond#
        [HttpPost]
        public async Task<IActionResult> Respond(PsyRespondViewModel model)
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account");

            var req = await _context.PsychologistRequests
                .FirstOrDefaultAsync(r => r.Id == model.Id && r.PsychologistId == psy.PsychologistId);
            if (req == null) return NotFound();
            if (req.Status != "Pending")
            {
                TempData["error"] = "Permintaan sudah ditanggapi sebelumnya.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Action == "Approve")
            {
                if (req.RequestType == "Worksheet")
                {
                    _context.Worksheets.Add(new Worksheet
                    {
                        PsychologistId = psy.PsychologistId,
                        PatientId = req.PatientId,
                        TaskName = string.IsNullOrWhiteSpace(req.ProposedTaskName) ? "Worksheet baru (dari HR)" : req.ProposedTaskName,
                        Description = req.Notes,
                        Deadline = req.ProposedDeadline ?? DateTime.Today.AddDays(7),
                        Status = "Assigned",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (req.RequestType == "Schedule")
                {
                    _context.Schedules.Add(new Schedule
                    {
                        PsychologistId = psy.PsychologistId,
                        PatientId = req.PatientId,
                        SessionStart = req.ProposedSessionDate ?? DateTime.UtcNow.AddDays(1),
                        DurationMinutes = 60,
                        Status = "Scheduled",
                        Notes = req.Notes
                    });
                }

                req.Status = "Approved";
                req.RespondedAt = DateTime.UtcNow;
                req.RespondedNote = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;
                await _context.SaveChangesAsync();
                TempData["success"] = $"Permintaan {req.RequestType} disetujui. Row terkait sudah dibuat.";
            }
            else if (model.Action == "Reject")
            {
                req.Status = "Rejected";
                req.RespondedAt = DateTime.UtcNow;
                req.RespondedNote = string.IsNullOrWhiteSpace(model.Note) ? "Ditolak tanpa catatan." : model.Note;
                await _context.SaveChangesAsync();
                TempData["success"] = "Permintaan ditolak.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
