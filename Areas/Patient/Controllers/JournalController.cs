using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class JournalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JournalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
        //  Daily Check-In — Intro
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult CheckIn() => View();

        [HttpPost, ActionName("CheckIn")]
        public IActionResult CheckInStart()
        {
            // Start at Q1 with all scores zero (server treats 0 as "not answered yet").
            return RedirectToAction(nameof(Question), new { step = 1 });
        }

        // ═════════════════════════════════════════════════════════════════
        //  Daily Check-In — Question 1..6
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Question(int step, JournalCheckInSessionViewModel model)
        {
            if (step < 1 || step > 6) return RedirectToAction(nameof(CheckIn));
            model.Step = step;
            return View(model);
        }

        [HttpPost, ActionName("Question")]
        public async Task<IActionResult> QuestionPost(JournalCheckInSessionViewModel model)
        {
            // Validate the score for the current step.
            int current = model.CurrentScore();
            if (current < 1 || current > 5)
            {
                ModelState.AddModelError("", "Pilih nilai 1 sampai 5.");
                return View("Question", model);
            }

            if (model.Step < 6)
            {
                // Move on with all accumulated scores in route values.
                return RedirectToAction(nameof(Question), new
                {
                    step = model.Step + 1,
                    FocusScore = model.FocusScore,
                    AnxietyScore = model.AnxietyScore,
                    SleepScore = model.SleepScore,
                    MindLoadScore = model.MindLoadScore,
                    EmotionScore = model.EmotionScore,
                    OverallScore = model.OverallScore
                });
            }

            // Step 6 just answered → save the row.
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var today = DateTime.Today;
            var existing = await _context.JournalCheckIns
                .FirstOrDefaultAsync(c => c.PatientId == patient.PatientId && c.CheckInDate.Date == today);

            if (existing == null)
            {
                _context.JournalCheckIns.Add(new JournalCheckIn
                {
                    PatientId = patient.PatientId,
                    FocusScore = model.FocusScore,
                    AnxietyScore = model.AnxietyScore,
                    SleepScore = model.SleepScore,
                    MindLoadScore = model.MindLoadScore,
                    EmotionScore = model.EmotionScore,
                    OverallScore = model.OverallScore,
                    CheckInDate = today,
                    RecordedAt = DateTime.Now
                });
            }
            else
            {
                existing.FocusScore = model.FocusScore;
                existing.AnxietyScore = model.AnxietyScore;
                existing.SleepScore = model.SleepScore;
                existing.MindLoadScore = model.MindLoadScore;
                existing.EmotionScore = model.EmotionScore;
                existing.OverallScore = model.OverallScore;
                existing.RecordedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CheckInSaved));
        }

        [HttpGet]
        public IActionResult CheckInSaved() => View();

        // ═════════════════════════════════════════════════════════════════
        //  Free-Write — one editable entry per day
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Write(int? id)
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            Journal? entry = null;
            if (id.HasValue)
            {
                entry = await _context.Journals
                    .FirstOrDefaultAsync(j => j.JournalId == id.Value && j.PatientId == patient.PatientId);
            }
            // No id specified → today's entry, or new
            entry ??= await _context.Journals
                .FirstOrDefaultAsync(j => j.PatientId == patient.PatientId && j.JournalDate.Date == DateTime.Today);

            var vm = new JournalWriteViewModel
            {
                JournalId = entry?.JournalId,
                Title = entry?.Title,
                Content = entry?.Content ?? string.Empty
            };
            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Write(JournalWriteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Beranda";
                return View(model);
            }

            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var today = DateTime.Today;
            Journal? entry = null;

            if (model.JournalId.HasValue)
            {
                entry = await _context.Journals
                    .FirstOrDefaultAsync(j => j.JournalId == model.JournalId.Value && j.PatientId == patient.PatientId);
            }
            entry ??= await _context.Journals
                .FirstOrDefaultAsync(j => j.PatientId == patient.PatientId && j.JournalDate.Date == today);

            if (entry == null)
            {
                entry = new Journal
                {
                    PatientId = patient.PatientId,
                    Title = model.Title,
                    Content = model.Content,
                    JournalDate = today,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Journals.Add(entry);
            }
            else
            {
                entry.Title = model.Title;
                entry.Content = model.Content;
                entry.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
