using LightenUp.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace LightenUp.Web.Areas.Patient.Controllers
{
// #Class JadwalController#
[Area("Patient")]
[Authorize(Roles = "Patient")]
public class JadwalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JadwalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // #Function Index#

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveNav = "Jadwal";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Schedules)
                    .ThenInclude(s => s.Psychologist)
                        .ThenInclude(psy => psy.User)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null) return NotFound("Patient record not found.");

            var vm = new JadwalViewModel();

            var allSchedules = patient.Schedules
                .Select(s => new JadwalItemViewModel
                {
                    ScheduleId = s.ScheduleId,
                    PsychologistName = s.Psychologist?.User?.FullName ?? "Dr. Unknown",
                    SessionStart = s.SessionStart,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status,
                    MeetingLink = s.MeetingLink
                })
                .OrderBy(s => s.SessionStart)
                .ToList();

            var today = DateTime.UtcNow;

            vm.UpcomingSessions = allSchedules.Where(s => s.SessionStart >= today || s.Status == "Scheduled").ToList();
            vm.PastSessions = allSchedules.Where(s => s.SessionStart < today && s.Status != "Scheduled").OrderByDescending(s => s.SessionStart).ToList();

            bool isB2B = patient.CompanyId != null;
            bool isMitra = !isB2B && patient.SponsorType == "Psychologist" && patient.SponsorPsychologistId != null;
            vm.IsB2B = isB2B;

            // Active psychologist assignment
            var activeAssignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");

            vm.HasActivePsychologist = activeAssignment != null;
            vm.PsychologistName = activeAssignment?.Psychologist?.User?.FullName;
            vm.PsychologistId = activeAssignment?.PsychologistId;

            if (activeAssignment != null)
            {
                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                vm.SessionsUsedThisMonth = await _context.Schedules
                    .CountAsync(s => s.PatientId == patient.PatientId
                        && s.SessionStart >= monthStart && s.SessionStart < monthEnd
                        && s.Status != "Cancelled");

                if (isB2B)
                {
                    var companySub = await _context.CompanySubscriptions
                        .Where(s => s.CompanyId == patient.CompanyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefaultAsync();

                    vm.IsSubscriptionExpired = companySub == null;
                    vm.MaxSessionsPerMonth = companySub?.MaxSessionsPerMonth ?? 4;
                    vm.SubscriptionEndDate = companySub?.EndDate;
                }
                else if (isMitra)
                {
                    var mitraSub = await _context.PsychologistSubscriptions
                        .Where(s => s.PsychologistId == patient.SponsorPsychologistId
                            && s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .FirstOrDefaultAsync();

                    if (mitraSub == null)
                    {
                        // Lazy cancel: psikolog tidak perpanjang → patient balik B2C
                        activeAssignment.Status = "Cancelled";
                        activeAssignment.CancellationReason = "Mitra subscription expired";
                        activeAssignment.CancellationRequestedAt = DateTime.UtcNow;
                        patient.SponsorType = "Self";
                        patient.SponsorPsychologistId = null;
                        await _context.SaveChangesAsync();

                        vm.HasActivePsychologist = false;
                        vm.PsychologistName = null;
                        vm.PsychologistId = null;
                        TempData["info"] = "Akses klinik Anda sudah berakhir. Silakan pilih psikolog baru atau masukkan kode referral dari psikolog klinik Anda.";
                    }
                    else
                    {
                        vm.IsSubscriptionExpired = false;
                        vm.MaxSessionsPerMonth = activeAssignment.Psychologist?.SessionTokensPerMonth ?? 4;
                        vm.SubscriptionEndDate = mitraSub.EndDate;
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

                    vm.IsSubscriptionExpired = activeSub == null;
                    vm.MaxSessionsPerMonth = activeSub?.MaxSessionsPerMonth
                        ?? activeAssignment.Psychologist?.SessionTokensPerMonth
                        ?? 4;
                    vm.SubscriptionEndDate = activeSub?.EndDate;
                }
            }

            // Pass to ViewBag for the modal partial
            ViewBag.PsychologistName = vm.PsychologistName;
            ViewBag.PsychologistId = vm.PsychologistId;
            ViewBag.PatientId = patient.PatientId;

            return View(vm);
        }

        // ─── Pasien meminta jadwal sesi konseling (gated: premium) ───
        // #Function RequestSession#
        // #Bagian Permintaan Jadwal#
        [HttpGet]
        public async Task<IActionResult> RequestSession()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            // Must have an active psychologist
            var assignment = await _context.Assignments
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (assignment == null)
            {
                TempData["error"] = "Anda belum memiliki psikolog aktif. Pilih psikolog terlebih dahulu.";
                return RedirectToAction("Index", "Psychologists");
            }

            ViewBag.PsychologistName = assignment.Psychologist?.User?.FullName ?? "—";
            ViewBag.PsychologistId = assignment.PsychologistId;
            ViewBag.PatientId = patient.PatientId;
            ViewBag.ActiveNav = "Jadwal";
            return View();
        }

        // #Function RequestSession POST#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSession(DateTime proposedDate, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null) return NotFound();

            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.PatientId == patient.PatientId && a.Status == "Active");
            if (assignment == null)
            {
                TempData["error"] = "Anda belum memiliki psikolog aktif.";
                return RedirectToAction(nameof(Index));
            }

            // Gate: cek validitas subscription dan token sesi
            bool patientIsB2B = patient.CompanyId != null;
            bool patientIsMitra = !patientIsB2B && patient.SponsorType == "Psychologist" && patient.SponsorPsychologistId != null;
            int maxTokens = 4;

            if (patientIsB2B)
            {
                var companySub = await _context.CompanySubscriptions
                    .Where(s => s.CompanyId == patient.CompanyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    .FirstOrDefaultAsync();
                if (companySub == null)
                {
                    TempData["error"] = "Akses Anda tidak aktif. Hubungi HR perusahaan untuk memperbarui langganan.";
                    return RedirectToAction(nameof(Index));
                }
                maxTokens = companySub.MaxSessionsPerMonth > 0 ? companySub.MaxSessionsPerMonth : 4;
            }
            else if (patientIsMitra)
            {
                var mitraSub = await _context.PsychologistSubscriptions
                    .Where(s => s.PsychologistId == patient.SponsorPsychologistId
                        && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    .FirstOrDefaultAsync();
                if (mitraSub == null)
                {
                    TempData["error"] = "Akses klinik Anda sudah berakhir. Hubungi psikolog klinik Anda.";
                    return RedirectToAction(nameof(Index));
                }
                var psyForToken = await _context.Psychologists.FindAsync(patient.SponsorPsychologistId);
                maxTokens = psyForToken?.SessionTokensPerMonth > 0 ? psyForToken.SessionTokensPerMonth : 4;
            }
            else
            {
                var activeSub = await _context.Subscriptions
                    .Where(s => s.PatientId == patient.PatientId
                        && s.PsychologistId == assignment.PsychologistId
                        && s.Status == "Active" && s.EndDate >= DateTime.Today)
                    .FirstOrDefaultAsync();
                if (activeSub == null)
                {
                    TempData["error"] = "Masa aktif langganan Anda sudah habis. Silakan pilih psikolog kembali.";
                    return RedirectToAction("Index", "Psychologists");
                }
                maxTokens = activeSub.MaxSessionsPerMonth > 0 ? activeSub.MaxSessionsPerMonth : 4;
            }

            var mStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var mEnd = mStart.AddMonths(1);
            var sessionsThisMonth = await _context.Schedules
                .CountAsync(s => s.PatientId == patient.PatientId
                    && s.SessionStart >= mStart && s.SessionStart < mEnd
                    && s.Status != "Cancelled");
            if (sessionsThisMonth >= maxTokens)
            {
                TempData["info"] = $"Token sesi bulan ini sudah habis ({sessionsThisMonth}/{maxTokens}). Tersedia lagi bulan depan.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent spamming
            var recentPending = await _context.PsychologistRequests.AnyAsync(r =>
                r.PatientId == patient.PatientId &&
                r.PsychologistId == assignment.PsychologistId &&
                r.RequestType == "Schedule" &&
                r.RequesterRole == "Patient" &&
                r.Status == "Pending");
            if (recentPending)
            {
                TempData["info"] = "Anda sudah memiliki permintaan jadwal yang sedang menunggu.";
                return RedirectToAction(nameof(Index));
            }

            _context.PsychologistRequests.Add(new PsychologistRequest
            {
                PatientId = patient.PatientId,
                PsychologistId = assignment.PsychologistId,
                RequestedByPatientUserId = user.Id,
                RequesterRole = "Patient",
                RequestType = "Schedule",
                ProposedSessionDate = proposedDate,
                Notes = notes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan jadwal sesi berhasil dikirim ke psikolog Anda.";
            return RedirectToAction(nameof(Index));
        }
    }
}
