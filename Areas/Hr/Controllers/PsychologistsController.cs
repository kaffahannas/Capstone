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
                    AlreadyPartnered = partneredIds.Contains(p.PsychologistId)
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
                AlreadyPartnered = partnered
            });
        }

        [HttpPost]
        public async Task<IActionResult> Pick(int id)
        {
            var hr = await GetHrAsync();
            if (hr == null || hr.CompanyId == null) return RedirectToAction("Welcome", "Onboarding");

            var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.PsychologistId == id && p.AcceptsB2B);
            if (psy == null) return NotFound();

            var company = await _context.Companies
                .Include(c => c.PartneredPsychologists)
                .FirstOrDefaultAsync(c => c.CompanyId == hr.CompanyId.Value);
            if (company == null) return NotFound();

            if (!company.PartneredPsychologists.Any(p => p.PsychologistId == id))
            {
                company.PartneredPsychologists.Add(psy);
                await _context.SaveChangesAsync();
                TempData["success"] = $"{psy.User?.FullName ?? "Psikolog"} ditambahkan ke daftar mitra perusahaan.";
            }
            else
            {
                TempData["success"] = "Psikolog sudah menjadi mitra perusahaan.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
