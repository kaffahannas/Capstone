using LightenUp.Web.Data;
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
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var hr = await _context.HrStaffs
                .Include(h => h.Company)
                .FirstOrDefaultAsync(h => h.UserId == user.Id);
            if (hr == null || hr.OnboardingCompletedAt == null)
            {
                return RedirectToAction("Welcome", "Onboarding");
            }

            var companyId = hr.CompanyId ?? 0;

            // ─── Company-scoped queries ───
            var patientsQ = _context.Patients
                .Include(p => p.User)
                .Where(p => p.CompanyId == companyId && p.EmploymentStatus == "active");

            var activeCount = await patientsQ.CountAsync();
            var sehat = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Sehat");
            var beresiko = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Beresiko");
            var bahaya = await patientsQ.CountAsync(p => p.MentalHealthStatus == "Bahaya");

            var divisions = await patientsQ
                .Where(p => p.Department != null && p.Department != "")
                .Select(p => p.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var preview = await patientsQ
                .OrderBy(p => p.User!.FullName)
                .Take(20)
                .Select(p => new HrClientPreview
                {
                    PatientId = p.PatientId,
                    FullName = p.User!.FullName,
                    Department = p.Department,
                    Status = p.MentalHealthStatus
                })
                .ToListAsync();

            // ─── Primary partnered psychologist (for "Kontak Psikolog" mailto) ───
            var psyEmail = await _context.Companies
                .Where(c => c.CompanyId == companyId)
                .SelectMany(c => c.PartneredPsychologists)
                .Include(p => p.User)
                .OrderBy(p => p.PsychologistId)
                .Select(p => p.User!.Email)
                .FirstOrDefaultAsync();

            var vm = new HrDashboardViewModel
            {
                ActiveCount = activeCount,
                SehatCount = sehat,
                BeresikoCount = beresiko,
                BahayaCount = bahaya,
                Divisions = divisions,
                ClientsPreview = preview,
                PrimaryPsychologistEmail = psyEmail,
                CompanyReferralCode = hr.Company?.ReferralCode
            };

            ViewBag.ActiveNav = "Beranda";
            return View(vm);
        }
    }
}
