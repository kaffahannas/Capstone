using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompaniesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var q = _context.Companies.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.Name.Contains(search));

            var companies = await q.OrderBy(c => c.Name).ToListAsync();

            var items = new List<AdminCompanyItem>();
            foreach (var c in companies)
            {
                items.Add(new AdminCompanyItem
                {
                    CompanyId = c.CompanyId,
                    Name = c.Name,
                    Address = c.Address,
                    ReferralCode = c.ReferralCode,
                    HrCount = await _context.HrStaffs.CountAsync(h => h.CompanyId == c.CompanyId),
                    PatientCount = await _context.Patients.CountAsync(p => p.CompanyId == c.CompanyId),
                    ActivePatientCount = await _context.Patients.CountAsync(p => p.CompanyId == c.CompanyId && p.EmploymentStatus == "active"),
                    CreatedAt = c.CreatedAt
                });
            }

            ViewBag.ActiveNav = "Companies";
            ViewData["Title"] = "Perusahaan";
            return View(new AdminCompaniesListViewModel { Search = search, Items = items });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var c = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (c == null) return NotFound();

            var hrs = await _context.HrStaffs.Include(h => h.User)
                .Where(h => h.CompanyId == id)
                .ToListAsync();
            var patients = await _context.Patients
                .Where(p => p.CompanyId == id)
                .ToListAsync();

            var vm = new AdminCompanyDetailViewModel
            {
                CompanyId = c.CompanyId,
                Name = c.Name,
                Address = c.Address,
                ContactNumber = c.ContactNumber,
                ContactEmail = c.ContactEmail,
                RegistrationNumber = c.RegistrationNumber,
                ReferralCode = c.ReferralCode,
                CreatedAt = c.CreatedAt,
                Hrs = hrs.Select(h => new AdminUserItem
                {
                    UserId = h.UserId,
                    FullName = h.User?.FullName ?? "—",
                    Email = h.User?.Email ?? "",
                    Role = "HR",
                    IsActive = h.User?.IsActive ?? false,
                    IsApprovedByAdmin = h.User?.IsApprovedByAdmin ?? false
                }).ToList(),
                TotalPatients = patients.Count,
                SehatCount = patients.Count(p => p.MentalHealthStatus == "Sehat"),
                BeresikoCount = patients.Count(p => p.MentalHealthStatus == "Beresiko"),
                BahayaCount = patients.Count(p => p.MentalHealthStatus == "Bahaya")
            };

            ViewBag.ActiveNav = "Companies";
            ViewData["Title"] = "Detail Perusahaan";
            return View(vm);
        }
    }
}
