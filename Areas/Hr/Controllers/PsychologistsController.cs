using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Hr.Controllers
{
    [Area("Hr")]
    [Authorize(Roles = "HR")]
    [RequiresCompanySubscription]
    public class PsychologistsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PsychologistsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<HrStaff?> GetHrAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.HrStaffs
                .Include(h => h.Company).ThenInclude(c => c!.PartneredPsychologists)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
        }

        // ═════════════════════════════════════════
        //  Directory — approved + AcceptsB2B
        // ═════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var partneredIds = hr.Company?.PartneredPsychologists.Select(p => p.PsychologistId).ToHashSet()
                               ?? new HashSet<int>();

            var psychologists = await _context.Psychologists
                .Include(p => p.User)
                .Where(p => p.AcceptsB2B && p.User != null && p.User.IsApprovedByAdmin)
                .OrderBy(p => p.User!.FullName)
                .ToListAsync();

            var pendingRequests = await _context.CompanyPsychologistRequests
                .Where(r => r.CompanyId == hr.CompanyId.Value && r.Status == "Pending" && r.PsychologistId != null)
                .Select(r => r.PsychologistId)
                .ToListAsync();

            var vm = new HrPsychologistDirectoryViewModel
            {
                Psychologists = psychologists.Select(p => new HrPsychologistCard
                {
                    PsychologistId = p.PsychologistId,
                    FullName = p.User?.FullName ?? "—",
                    Specialization = p.Specialization,
                    ProfilePicture = p.User?.ProfilePicture,
                    ExperienceYears = p.ExperienceYears,
                    Email = p.User?.Email,
                    Bio = p.Bio,
                    University = p.University,
                    LastDegree = p.LastDegree,
                    PracticeLocation = p.PracticeLocation,
                    AlreadyPartnered = partneredIds.Contains(p.PsychologistId),
                    HasPendingRequest = pendingRequests.Contains(p.PsychologistId)
                }).ToList()
            };

            ViewBag.ActiveNav = "Psikolog";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Profile(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var p = await _context.Psychologists
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.PsychologistId == id && x.AcceptsB2B);
            if (p == null) return NotFound();

            var partnered = hr.Company?.PartneredPsychologists.Any(c => c.PsychologistId == id) ?? false;
            var hasPending = await _context.CompanyPsychologistRequests.AnyAsync(r => r.CompanyId == hr.CompanyId.Value && r.PsychologistId == id && r.Status == "Pending");

            ViewBag.ActiveNav = "Psikolog";
            return View(new HrPsychologistProfileViewModel
            {
                PsychologistId = p.PsychologistId,
                FullName = p.User?.FullName ?? "—",
                Specialization = p.Specialization,
                Bio = p.Bio,
                University = p.University,
                LastDegree = p.LastDegree,
                ExperienceYears = p.ExperienceYears,
                PracticeLocation = p.PracticeLocation,
                ProfilePicture = p.User?.ProfilePicture,
                Email = p.User?.Email,
                AlreadyPartnered = partnered,
                HasPendingRequest = hasPending
            });
        }

        [HttpPost]
        public async Task<IActionResult> RequestPartnership(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.PsychologistId == id && p.AcceptsB2B);
            if (psy == null) return NotFound();

            var company = await _context.Companies
                .Include(c => c.PartneredPsychologists)
                .FirstOrDefaultAsync(c => c.CompanyId == hr.CompanyId.Value);
            if (company == null) return NotFound();

            if (company.PartneredPsychologists.Any(p => p.PsychologistId == id))
            {
                TempData["info"] = "Psikolog sudah menjadi mitra perusahaan.";
                return RedirectToAction(nameof(Index));
            }

            var existingRequest = await _context.CompanyPsychologistRequests
                .FirstOrDefaultAsync(r => r.CompanyId == company.CompanyId && r.PsychologistId == id && r.Status == "Pending");
            
            if (existingRequest != null)
            {
                TempData["info"] = "Permintaan kemitraan sudah diajukan dan sedang menunggu persetujuan.";
                return RedirectToAction(nameof(Index));
            }

            var request = new CompanyPsychologistRequest
            {
                CompanyId = company.CompanyId,
                PsychologistId = id,
                Status = "Pending"
            };

            _context.CompanyPsychologistRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["success"] = $"Permintaan kemitraan kepada {psy.User?.FullName ?? "Psikolog"} berhasil dikirim.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RequestFromAdmin(string? notes)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var existingRequest = await _context.CompanyPsychologistRequests
                .FirstOrDefaultAsync(r => r.CompanyId == hr.CompanyId.Value && r.PsychologistId == null && r.Status == "Pending");
            
            if (existingRequest != null)
            {
                TempData["info"] = "Permintaan psikolog ke admin sudah diajukan dan sedang menunggu proses.";
                return RedirectToAction(nameof(Index));
            }

            var request = new CompanyPsychologistRequest
            {
                CompanyId = hr.CompanyId.Value,
                PsychologistId = null,
                Status = "Pending",
                Notes = notes
            };

            _context.CompanyPsychologistRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan psikolog ke admin berhasil dikirim.";

            return RedirectToAction(nameof(Index));
        }

        // ─── HR: melihat permintaan pembatalan dari psikolog (B2B) ───
        [HttpGet]
        public async Task<IActionResult> PendingCancellations()
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var pending = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .Include(a => a.CancellationRequestedBy)
                .Where(a => a.Status == "PendingCancellationByHr"
                         && a.Patient!.CompanyId == hr.CompanyId.Value)
                .ToListAsync();

            ViewBag.ActiveNav = "PendingCancellations";
            ViewData["Title"] = "Permintaan Pembatalan Kemitraan";
            return View(pending);
        }

        // ─── HR menyetujui pembatalan B2B partnership ───
        [HttpPost]
        public async Task<IActionResult> ApproveCancellation(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null) return Forbid();

            var a = await _context.Assignments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId &&
                                         a.Status == "PendingCancellationByHr" &&
                                         a.Patient!.CompanyId == hr.CompanyId);
            if (a == null) return NotFound();

            a.Status = "Cancelled";
            a.DecisionByUserId = user?.Id;
            a.DecisionAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["success"] = "Pembatalan kemitraan psikolog disetujui.";
            return RedirectToAction(nameof(PendingCancellations));
        }

        // ─── HR menolak pembatalan B2B partnership ───
        [HttpPost]
        public async Task<IActionResult> RejectCancellation(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var hr = await GetHrAsync();
            if (hr == null) return Forbid();

            var a = await _context.Assignments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId &&
                                         a.Status == "PendingCancellationByHr" &&
                                         a.Patient!.CompanyId == hr.CompanyId);
            if (a == null) return NotFound();

            a.Status = "Active";
            a.DecisionByUserId = user?.Id;
            a.DecisionAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pembatalan ditolak. Penugasan tetap aktif.";
            return RedirectToAction(nameof(PendingCancellations));
        }
    }
}

