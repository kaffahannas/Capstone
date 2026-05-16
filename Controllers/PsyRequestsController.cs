using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Controllers
{
    [Authorize(Roles = "Psychologist")]
    public class PsyRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PsyRequestsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Psychologist?> GetPsyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ═════════════════════════════════════════
        //  Inbox
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "Pending")
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account");

            var q = _context.PsychologistRequests
                .Include(r => r.Patient).ThenInclude(p => p!.User)
                .Include(r => r.Patient).ThenInclude(p => p!.Company)
                .Include(r => r.RequestedByHr)
                .Where(r => r.PsychologistId == psy.PsychologistId);

            if (tab is "Pending" or "Approved" or "Rejected")
                q = q.Where(r => r.Status == tab);

            var items = await q.OrderByDescending(r => r.CreatedAt).Select(r => new PsyRequestListItem
            {
                Id = r.Id,
                RequestType = r.RequestType,
                PatientName = r.Patient!.User!.FullName,
                CompanyName = r.Patient.Company!.Name,
                HrName = r.RequestedByHr!.FullName,
                Notes = r.Notes,
                ProposedTaskName = r.ProposedTaskName,
                ProposedDeadline = r.ProposedDeadline,
                ProposedSessionDate = r.ProposedSessionDate,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                RespondedNote = r.RespondedNote
            }).ToListAsync();

            return View(new PsyRequestsViewModel { Tab = tab, Items = items });
        }

        // ═════════════════════════════════════════
        //  Respond — Approve creates the actual row, Reject just records the rejection
        // ═════════════════════════════════════════
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
                        CreatedAt = DateTime.Now
                    });
                }
                else if (req.RequestType == "Schedule")
                {
                    _context.Schedules.Add(new Schedule
                    {
                        PsychologistId = psy.PsychologistId,
                        PatientId = req.PatientId,
                        SessionStart = req.ProposedSessionDate ?? DateTime.Now.AddDays(1),
                        DurationMinutes = 60,
                        Status = "Scheduled",
                        Notes = req.Notes
                    });
                }

                req.Status = "Approved";
                req.RespondedAt = DateTime.Now;
                req.RespondedNote = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;
                await _context.SaveChangesAsync();
                TempData["success"] = $"Permintaan {req.RequestType} disetujui. Row terkait sudah dibuat.";
            }
            else if (model.Action == "Reject")
            {
                req.Status = "Rejected";
                req.RespondedAt = DateTime.Now;
                req.RespondedNote = string.IsNullOrWhiteSpace(model.Note) ? "Ditolak tanpa catatan." : model.Note;
                await _context.SaveChangesAsync();
                TempData["success"] = "Permintaan ditolak.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
