using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    // Flow: Feeling → Triggers → Note → Question(1-5) → Summary → Save
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

        // ═══ Step 1 — Feeling ════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Feeling()
        {
            var patient = await GetPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            var today = DateTime.Today;
            var existing = await _context.MoodTrackers
                .FirstOrDefaultAsync(m => m.PatientId == patient.PatientId && m.MoodDate.Date == today);

            var vm = new MoodTrackerSessionViewModel();
            if (existing != null)
            {
                vm.Feeling       = existing.Feeling;
                vm.Triggers      = string.IsNullOrEmpty(existing.Triggers) ? new() : existing.Triggers.Split(',').ToList();
                vm.Note          = existing.Note;
                vm.FocusScore    = existing.FocusScore    ?? 0;
                vm.AnxietyScore  = existing.AnxietyScore  ?? 0;
                vm.SleepScore    = existing.SleepScore    ?? 0;
                vm.MindLoadScore = existing.MindLoadScore ?? 0;
                vm.EmotionScore  = existing.EmotionScore  ?? 0;
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

        // ═══ Step 2 — Triggers ═══════════════════════════════════════════
        [HttpGet]
        public IActionResult Triggers(string feeling, string? triggers, string? note,
            int focusScore = 0, int anxietyScore = 0, int sleepScore = 0, int mindLoadScore = 0, int emotionScore = 0)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling, Note = note,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                FocusScore = focusScore, AnxietyScore = anxietyScore,
                SleepScore = sleepScore, MindLoadScore = mindLoadScore, EmotionScore = emotionScore
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

        // ═══ Step 3 — Note (optional) ════════════════════════════════════
        [HttpGet]
        public IActionResult Note(string feeling, string? triggers, string? note,
            int focusScore = 0, int anxietyScore = 0, int sleepScore = 0, int mindLoadScore = 0, int emotionScore = 0)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling, Note = note,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                FocusScore = focusScore, AnxietyScore = anxietyScore,
                SleepScore = sleepScore, MindLoadScore = mindLoadScore, EmotionScore = emotionScore
            });
        }

        [HttpPost, ActionName("Note")]
        public IActionResult NotePost(MoodTrackerSessionViewModel model)
        {
            model.QuestionStep = 1;
            return RedirectToAction(nameof(Question), MakeRouteValues(model));
        }

        // ═══ Steps 4-8 — Questionnaire (5 questions, score 1-5) ══════════
        [HttpGet]
        public IActionResult Question(string feeling, string? triggers, string? note, int questionStep = 1,
            int focusScore = 0, int anxietyScore = 0, int sleepScore = 0, int mindLoadScore = 0, int emotionScore = 0)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            if (questionStep < 1 || questionStep > 5) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling, Note = note, QuestionStep = questionStep,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                FocusScore = focusScore, AnxietyScore = anxietyScore,
                SleepScore = sleepScore, MindLoadScore = mindLoadScore, EmotionScore = emotionScore
            });
        }

        [HttpPost, ActionName("Question")]
        public IActionResult QuestionPost(MoodTrackerSessionViewModel model)
        {
            int score = model.CurrentQuestionScore();
            if (score < 1 || score > 5)
            {
                ModelState.AddModelError("", "Pilih nilai 1 sampai 5.");
                ViewBag.ActiveNav = "Beranda";
                return View(model);
            }

            if (model.QuestionStep < 5)
            {
                model.QuestionStep++;
                return RedirectToAction(nameof(Question), MakeRouteValues(model));
            }

            // All 5 done → summary
            return RedirectToAction(nameof(Summary), MakeRouteValues(model));
        }

        // ═══ Summary + Save ═══════════════════════════════════════════════
        [HttpGet]
        public IActionResult Summary(string feeling, string? triggers, string? note,
            int focusScore = 0, int anxietyScore = 0, int sleepScore = 0, int mindLoadScore = 0, int emotionScore = 0)
        {
            if (string.IsNullOrEmpty(feeling)) return RedirectToAction(nameof(Feeling));
            ViewBag.ActiveNav = "Beranda";
            return View(new MoodTrackerSessionViewModel
            {
                Feeling = feeling, Note = note,
                Triggers = string.IsNullOrEmpty(triggers) ? new() : triggers.Split(',').ToList(),
                FocusScore = focusScore, AnxietyScore = anxietyScore,
                SleepScore = sleepScore, MindLoadScore = mindLoadScore, EmotionScore = emotionScore
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

            static int? ToNullable(int v) => v > 0 ? v : null;

            if (existing == null)
            {
                _context.MoodTrackers.Add(new MoodTracker
                {
                    PatientId     = patient.PatientId,
                    Feeling       = model.Feeling,
                    Triggers      = string.Join(",", model.Triggers ?? new()),
                    Note          = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note,
                    FocusScore    = ToNullable(model.FocusScore),
                    AnxietyScore  = ToNullable(model.AnxietyScore),
                    SleepScore    = ToNullable(model.SleepScore),
                    MindLoadScore = ToNullable(model.MindLoadScore),
                    EmotionScore  = ToNullable(model.EmotionScore),
                    MoodDate      = today,
                    RecordedAt    = DateTime.Now
                });
            }
            else
            {
                existing.Feeling       = model.Feeling;
                existing.Triggers      = string.Join(",", model.Triggers ?? new());
                existing.Note          = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note;
                existing.FocusScore    = ToNullable(model.FocusScore);
                existing.AnxietyScore  = ToNullable(model.AnxietyScore);
                existing.SleepScore    = ToNullable(model.SleepScore);
                existing.MindLoadScore = ToNullable(model.MindLoadScore);
                existing.EmotionScore  = ToNullable(model.EmotionScore);
                existing.RecordedAt    = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }

        // ═══ Helper ═══════════════════════════════════════════════════════
        private static object MakeRouteValues(MoodTrackerSessionViewModel m) => new
        {
            feeling       = m.Feeling,
            triggers      = string.Join(",", m.Triggers ?? new()),
            note          = m.Note,
            questionStep  = m.QuestionStep,
            focusScore    = m.FocusScore,
            anxietyScore  = m.AnxietyScore,
            sleepScore    = m.SleepScore,
            mindLoadScore = m.MindLoadScore,
            emotionScore  = m.EmotionScore
        };
    }
}
