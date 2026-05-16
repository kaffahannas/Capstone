using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    // 4-step mood tracker. State flows through hidden fields so users can navigate
    // back/forth freely. Save only happens at step 4 (Summary).
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class MoodController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MoodController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<LightenUp.Web.Models.Patient?> GetPatientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 1/4 — Feeling
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Feeling()
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            // If today's mood already exists, preload it (Edit flow)
            var today = DateTime.Today;
            var existing = await _context.MoodTrackers
                .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today);

            var vm = new MoodTrackerSessionViewModel();
            if (existing != null)
            {
                vm.Feeling = existing.Feeling;
                vm.Triggers = string.IsNullOrEmpty(existing.Triggers)
                    ? new()
                    : existing.Triggers.Split(',').ToList();
                vm.Note = existing.Note;
            }

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }

        [HttpPost]
        public IActionResult Feeling(MoodTrackerSessionViewModel model)
        {
            if (string.IsNullOrEmpty(model.Feeling))
            {
                ModelState.AddModelError(nameof(model.Feeling), "Pilih salah satu perasaan.");
                ViewBag.ActiveNav = "Beranda";
                return View(model);
            }
            return RedirectToAction(nameof(Triggers), MakeRouteValues(model));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 2/4 — Triggers
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Triggers(string feeling, string? triggers, string? note)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                Note = note
            });
        }

        [HttpPost, ActionName("Triggers")]
        public IActionResult TriggersPost(MoodTrackerSessionViewModel model)
        {
            if (model.Triggers == null || model.Triggers.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Triggers), "Pilih minimal satu pemicu.");
                ViewBag.ActiveNav = "Beranda";
                return View(model);
            }
            return RedirectToAction(nameof(Note), MakeRouteValues(model));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 3/4 — Note (optional)
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Note(string feeling, string? triggers, string? note)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                Note = note
            });
        }

        [HttpPost, ActionName("Note")]
        public IActionResult NotePost(MoodTrackerSessionViewModel model)
        {
            return RedirectToAction(nameof(Summary), MakeRouteValues(model));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 4/4 — Summary + Save
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Summary(string feeling, string? triggers, string? note)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                Note = note
            });
        }

        [HttpPost, ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(MoodTrackerSessionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Beranda";
                return View(model);
            }

            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var today = DateTime.Today;
            var existing = await _context.MoodTrackers
                .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today);

            if (existing == null)
            {
                _context.MoodTrackers.Add(new MoodTracker
                {
                    PatientId = patient.PatientId,
                    Feeling = model.Feeling,
                    Triggers = string.Join(",", model.Triggers ?? new()),
                    Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note,
                    MoodDate = today,
                    RecordedAt = DateTime.Now
                });
            }
            else
            {
                existing.Feeling = model.Feeling;
                existing.Triggers = string.Join(",", model.Triggers ?? new());
                existing.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;
                existing.RecordedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }

        // ═════════════════════════════════════════════════════════════════
        //  Helper — pack model into route values for next step's GET
        // ═════════════════════════════════════════════════════════════════
        private static object MakeRouteValues(MoodTrackerSessionViewModel m) => new
        {
            feeling = m.Feeling,
            triggers = string.Join(",", m.Triggers ?? new()),
            note = m.Note
        };
    }
}
