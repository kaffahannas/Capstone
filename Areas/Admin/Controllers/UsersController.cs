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
            var q = _userManager.Users.Where(u => u.RoleType != "Admin").AsQueryable();

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
                var p = await _context.Patients.Include(p => p.Company).Include(p => p.Division).FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (p != null)
                {
                    vm.CompanyName = p.Company?.Name;
                    vm.Department = p.DivisionId == null ? "Belum Diatur" : p.Division?.Name;
                    vm.MentalHealthStatus = p.MentalHealthStatus;
                    vm.DateOfBirth = p.DateOfBirth;
                    vm.Gender = p.Gender;
                    vm.EmergencyContactName = p.EmergencyContactName;
                    vm.EmergencyContactPhone = p.EmergencyContactPhone;
                }
            }
            else if (user.RoleType == "Psychologist")
            {
                var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (psy != null)
                {
                    vm.Specialization = psy.Specialization;
                    vm.LicenseNumber = psy.LicenseNumber;
                    vm.SiapNumber = psy.SiapNumber;
                    vm.LastDegree = psy.LastDegree;
                    vm.University = psy.University;
                    vm.ExperienceYears = psy.ExperienceYears;
                    vm.PracticeLocation = psy.PracticeLocation;
                    vm.AcademicDocumentUrl = psy.AcademicDocumentUrl;
                    vm.StrDocumentUrl = psy.StrDocumentUrl;
                }
            }
            else if (user.RoleType == "HR")
            {
                var hr = await _context.HrStaffs.Include(h => h.Company).FirstOrDefaultAsync(h => h.UserId == user.Id);
                if (hr != null)
                {
                    vm.CompanyName = hr.Company?.Name;
                    vm.Department = hr.Department;
                    vm.CompanyAddress = hr.Company?.Address;
                    vm.CompanyRegistrationNumber = hr.Company?.RegistrationNumber;
                    vm.SupportDocumentUrl = hr.SupportDocumentUrl;
                    vm.LastDegree = hr.LastDegree;
                    vm.University = hr.University;
                    vm.AcademicDocumentUrl = hr.AcademicDocumentUrl;
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


        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new AdminUserEditViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                Role = user.RoleType
            };

            ViewBag.ActiveNav = "Users";
            ViewData["Title"] = "Edit Pengguna";
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AdminUserEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ActiveNav = "Users";
                ViewData["Title"] = "Edit Pengguna";
                return View(vm);
            }

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            user.FullName = vm.FullName;
            user.Email = vm.Email;
            user.UserName = vm.Email;
            user.PhoneNumber = vm.Phone;

            // Handle role change if needed (remove old role, add new role)
            if (user.RoleType != vm.Role)
            {
                var oldRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, oldRoles);
                await _userManager.AddToRoleAsync(user, vm.Role);
                user.RoleType = vm.Role;
                
                // If it's psychologist or hr, we might need to do cleanups, but for simplicity we just update the identity role.
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["success"] = "Data pengguna berhasil diperbarui.";
                return RedirectToAction(nameof(Detail), new { id = user.Id });
            }

            foreach (var err in result.Errors) ModelState.AddModelError("", err.Description);
            ViewBag.ActiveNav = "Users";
            ViewData["Title"] = "Edit Pengguna";
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Additional logic to delete from Patient/Psychologist/HR tables if necessary.
            // But EF Core Cascade Delete should handle it if configured, or we can just delete the identity user.
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                TempData["success"] = $"Pengguna {user.FullName} berhasil dihapus permanen.";
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = "Gagal menghapus pengguna: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}
