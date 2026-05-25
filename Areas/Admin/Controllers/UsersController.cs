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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1)
        {
            var q = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.FullName.Contains(search) || (u.Email != null && u.Email.Contains(search)));
            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.RoleType == role);
            if (status == "Active") q = q.Where(u => u.IsActive);
            else if (status == "Inactive") q = q.Where(u => !u.IsActive);
            else if (status == "Pending") q = q.Where(u => !u.IsApprovedByAdmin);

            const int pageSize = 20;
            var total = await q.CountAsync();
            var rows = await q.OrderBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new AdminUsersListViewModel
            {
                Search = search,
                Role = role,
                Status = status,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = rows.Select(u => new AdminUserItem
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = u.RoleType,
                    IsActive = u.IsActive,
                    IsApprovedByAdmin = u.IsApprovedByAdmin
                }).ToList()
            };

            ViewBag.ActiveNav = "Users";
            ViewData["Title"] = "Pengguna";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new AdminUserDetailViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                Role = user.RoleType,
                IsActive = user.IsActive,
                IsApprovedByAdmin = user.IsApprovedByAdmin,
                ProfilePicture = user.ProfilePicture
            };

            if (user.RoleType == "Patient")
            {
                var p = await _context.Patients.Include(p => p.Company).FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (p != null)
                {
                    vm.CompanyName = p.Company?.Name;
                    vm.Department = p.Department;
                    vm.MentalHealthStatus = p.MentalHealthStatus;
                }
            }
            else if (user.RoleType == "Psychologist")
            {
                var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (psy != null)
                {
                    vm.Specialization = psy.Specialization;
                    vm.LicenseNumber = psy.LicenseNumber;
                }
            }
            else if (user.RoleType == "HR")
            {
                var hr = await _context.HrStaffs.Include(h => h.Company).FirstOrDefaultAsync(h => h.UserId == user.Id);
                if (hr != null)
                {
                    vm.CompanyName = hr.Company?.Name;
                    vm.Department = hr.Department;
                }
            }

            ViewBag.ActiveNav = "Users";
            ViewData["Title"] = "Detail Pengguna";
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["success"] = $"{user.FullName} {(user.IsActive ? "diaktifkan" : "dinonaktifkan")}.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["error"] = "Kata sandi minimal 8 karakter.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
                TempData["success"] = $"Kata sandi {user.FullName} direset.";
            else
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
