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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = await _userManager.Users.ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                TotalPatients = allUsers.Count(u => u.RoleType == "Patient"),
                TotalPsychologists = allUsers.Count(u => u.RoleType == "Psychologist"),
                TotalHrs = allUsers.Count(u => u.RoleType == "HR"),
                TotalAdmins = allUsers.Count(u => u.RoleType == "Admin"),
                TotalCompanies = await _context.Companies.CountAsync(),
                PendingPsychologists = allUsers.Count(u => u.RoleType == "Psychologist" && !u.IsApprovedByAdmin),
                PendingHrs = 0,
                PendingAdminAssignments = await _context.Assignments.CountAsync(a => a.Status == "PendingAdminApproval"),
                PendingCancellationByAdmin = await _context.Assignments.CountAsync(a => a.Status == "PendingCancellationByAdmin"),
            };

            ViewBag.ActiveNav = "Dashboard";
            ViewData["Title"] = "Dashboard";
            return View(vm);
        }
    }
}
