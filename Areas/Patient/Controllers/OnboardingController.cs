using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    // Patient onboarding — 10-step survey (Welcome → Gender → ... → Terms).
    // Resume logic: if any field is unset, login redirects here at the next unanswered step.
    // The "X" button on each screen saves nothing and bounces the user to /Account/Logout
    // (or to the dashboard if onboarding is already complete).
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionAccessService _access;

        public OnboardingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _access = access;
        }

        // ───── helper: load current patient (creates the row if for some reason it's missing) ─────
        private async Task<LightenUp.Web.Models.Patient?> GetCurrentPatientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null)
            {
                patient = new LightenUp.Web.Models.Patient { UserId = user.Id };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
            return patient;
        }

        // ───── helper: figure out which step is the first unanswered one ─────
        // Used to route the resume on login, and to redirect a user who jumps ahead in URL.
        private static string NextStepFor(LightenUp.Web.Models.Patient p)
        {
            if (string.IsNullOrEmpty(p.Gender)) return nameof(Gender);
            if (p.DateOfBirth == null) return nameof(Birthdate);
            if (string.IsNullOrEmpty(p.RelationshipStatus)) return nameof(Relationship);
            if (string.IsNullOrEmpty(p.SpiritualActivity)) return nameof(Spiritual);
            if (p.HasPreviousCounseling == null) return nameof(CounselingHistory);
            if (p.HasMedicationHistory == null) return nameof(Medication);
            if (string.IsNullOrEmpty(p.SleepQuality)) return nameof(SleepQuality);
            if (string.IsNullOrEmpty(p.AppGoals)) return nameof(AppGoals);
            // ReferralCode is optional — never blocks. Skip directly to Terms after AppGoals.
            if (p.TermsAcceptedAt == null) return nameof(ReferralCode); // ReferralCode → Terms
            return nameof(Terms);
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 0 — Welcome
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Welcome()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Already finished? Send to dashboard.
            if (patient.OnboardingCompletedAt != null)
                return RedirectToAction("Index", "Dashboard");

            ViewBag.PatientName = (await _userManager.GetUserAsync(User))?.FullName ?? "";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Welcome(bool _)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            return RedirectToAction(NextStepFor(patient));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 1 — Gender
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Gender()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 1 };
            return View(new OnboardingGenderViewModel { Gender = patient.Gender ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> Gender(OnboardingGenderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 1 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.Gender = model.Gender;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Birthdate));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 2 — Birthdate
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Birthdate()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 2 };
            var vm = new OnboardingBirthdateViewModel();
            if (patient.DateOfBirth.HasValue)
            {
                vm.Day = patient.DateOfBirth.Value.Day;
                vm.Month = patient.DateOfBirth.Value.Month;
                vm.Year = patient.DateOfBirth.Value.Year;
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Birthdate(OnboardingBirthdateViewModel model)
        {
            var date = model.AsDate();
            if (date == null)
            {
                ModelState.AddModelError("", "Tanggal lahir tidak valid.");
            }
            else
            {
                var age = DateTime.Today.Year - date.Value.Year;
                if (date.Value.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 13)
                    ModelState.AddModelError("", "Usia minimal 13 tahun untuk menggunakan LightenUp.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 2 };
                return View(model);
            }

            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.DateOfBirth = date;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Relationship));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 3 — Relationship
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Relationship()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 3 };
            return View(new OnboardingRelationshipViewModel { RelationshipStatus = patient.RelationshipStatus ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> Relationship(OnboardingRelationshipViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 3 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.RelationshipStatus = model.RelationshipStatus;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Spiritual));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 4 — Spiritual
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Spiritual()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 4 };
            return View(new OnboardingSpiritualViewModel { SpiritualActivity = patient.SpiritualActivity ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> Spiritual(OnboardingSpiritualViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 4 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.SpiritualActivity = model.SpiritualActivity;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CounselingHistory));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 5 — Counseling history (with conditional follow-up)
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CounselingHistory()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 5 };
            var vm = new OnboardingCounselingViewModel
            {
                HasPreviousCounseling = patient.HasPreviousCounseling,
                CounselingMethods = string.IsNullOrEmpty(patient.CounselingMethods)
                    ? new List<string>()
                    : patient.CounselingMethods.Split(',').ToList(),
                CounselingMethodOther = patient.CounselingMethodOther
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CounselingHistory(OnboardingCounselingViewModel model)
        {
            // If Pernah is selected, at least one method must be chosen.
            if (model.HasPreviousCounseling == true && (model.CounselingMethods == null || model.CounselingMethods.Count == 0))
            {
                ModelState.AddModelError("CounselingMethods", "Pilih minimal satu metode konseling yang pernah dilakukan.");
            }
            if (model.HasPreviousCounseling == true
                && model.CounselingMethods != null
                && model.CounselingMethods.Contains("Other")
                && string.IsNullOrWhiteSpace(model.CounselingMethodOther))
            {
                ModelState.AddModelError("CounselingMethodOther", "Jelaskan metode lainnya.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 5 };
                return View(model);
            }

            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.HasPreviousCounseling = model.HasPreviousCounseling;
            patient.CounselingMethods = model.HasPreviousCounseling == true
                ? string.Join(",", model.CounselingMethods ?? new())
                : null;
            patient.CounselingMethodOther = model.HasPreviousCounseling == true ? model.CounselingMethodOther : null;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Medication));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 6 — Medication
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Medication()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 6 };
            return View(new OnboardingMedicationViewModel { HasMedicationHistory = patient.HasMedicationHistory });
        }

        [HttpPost]
        public async Task<IActionResult> Medication(OnboardingMedicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 6 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.HasMedicationHistory = model.HasMedicationHistory;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(SleepQuality));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 7 — Sleep quality
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> SleepQuality()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 7 };
            return View(new OnboardingSleepViewModel { SleepQuality = patient.SleepQuality ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> SleepQuality(OnboardingSleepViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 7 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.SleepQuality = model.SleepQuality;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AppGoals));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 8 — App goals (multi-select)
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> AppGoals()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 8 };
            var vm = new OnboardingAppGoalsViewModel
            {
                AppGoals = string.IsNullOrEmpty(patient.AppGoals)
                    ? new List<string>()
                    : patient.AppGoals.Split(',').ToList()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AppGoals(OnboardingAppGoalsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 8 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.AppGoals = string.Join(",", model.AppGoals);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ReferralCode));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 9 — Referral code (optional; can also be set later from profile)
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ReferralCode()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 9 };
            return View(new OnboardingReferralViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ReferralCode(OnboardingReferralViewModel model)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (!string.IsNullOrWhiteSpace(model.ReferralCode))
            {
                var code = model.ReferralCode.Trim();
                var division = await _context.CompanyDivisions.FirstOrDefaultAsync(d => d.ReferralCode == code);
                if (division == null)
                {
                    ModelState.AddModelError("ReferralCode", "Kode referral tidak ditemukan. Anda dapat mengisinya nanti di profil.");
                    ViewBag.Progress = new OnboardingProgress { Current = 9 };
                    return View(model);
                }

                if (!await _access.CanUseReferralCodeAsync(division.CompanyId))
                {
                    ModelState.AddModelError("ReferralCode", "Langganan perusahaan belum aktif atau sudah berakhir. Hubungi HR perusahaan Anda.");
                    ViewBag.Progress = new OnboardingProgress { Current = 9 };
                    return View(model);
                }

                patient.CompanyId = division.CompanyId;
                patient.Department = division.Name;

                // Auto-claim a PendingEmployee row (HR pre-registered this email) and copy EmployeeId.
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var pending = await _context.PendingEmployees
                        .FirstOrDefaultAsync(pe => pe.CompanyId == division.CompanyId
                            && pe.Email == user.Email
                            && pe.ClaimedByPatientId == null);
                    if (pending != null)
                    {
                        patient.Department = pending.Department;
                        patient.EmployeeId = pending.EmployeeId;
                        pending.ClaimedByPatientId = patient.PatientId;
                        pending.ClaimedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Terms));
        }

        // ═════════════════════════════════════════════════════════════════
        //  Step 10 — Terms & Conditions → completes onboarding
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Terms()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });
            if (patient.OnboardingCompletedAt != null) return RedirectToAction("Index", "Dashboard");

            ViewBag.Progress = new OnboardingProgress { Current = 10 };
            return View(new OnboardingTermsViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Terms(OnboardingTermsViewModel model)
        {
            if (!model.Accepted)
            {
                ModelState.AddModelError(nameof(model.Accepted), "Anda harus menyetujui Syarat & Ketentuan.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Progress = new OnboardingProgress { Current = 10 };
                return View(model);
            }
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Login", "Account", new { area = "" });

            patient.TermsAcceptedAt = DateTime.UtcNow;
            patient.OnboardingCompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
