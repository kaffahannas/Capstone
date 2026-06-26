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
    // #Class UsersController#
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

        // #Function Index#

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

        // #Function Detail#

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
                var psy = await _context.Psychologists
                    .Include(p => p.MitraSubscriptions)
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
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
                    vm.PsychologistId = psy.PsychologistId;
                    vm.IsMitraActive = psy.IsMitraActive;
                    var activeMitra = psy.MitraSubscriptions
                        .Where(s => s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .OrderByDescending(s => s.EndDate).FirstOrDefault();
                    vm.MitraEndDate = activeMitra?.EndDate;
                }
            }
            else if (user.RoleType == "HR")
            {
                var hr = await _context.HrStaffs
                    .Include(h => h.Company).ThenInclude(c => c!.Subscriptions)
                    .FirstOrDefaultAsync(h => h.UserId == user.Id);
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
                    vm.CompanyId = hr.Company?.CompanyId;
                    var activeSub = hr.Company?.Subscriptions
                        .Where(s => s.Status == "Active" && s.EndDate >= DateTime.Today)
                        .OrderByDescending(s => s.EndDate).FirstOrDefault();
                    vm.IsCompanySubscriptionActive = activeSub != null;
                    vm.CompanySubscriptionEndDate = activeSub?.EndDate;
                }
            }

            ViewBag.ActiveNav = "Users";
            ViewData["Title"] = "Detail Pengguna";
            return View(vm);
        }

        // #Function ToggleActive#

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


        // Plans mirrored from MitraController and Hr/SubscriptionController
        private static readonly (string PlanId, string Name, int DurationMonths, int PatientLimit)[] MitraPlans =
        {
            ("mitra-10", "Mitra 10 Klien (1 bln)",  1, 10),
            ("mitra-25", "Mitra 25 Klien (1 bln)",  1, 25),
            ("mitra-50", "Mitra 50 Klien (1 bln)",  1, 50),
        };
        private static readonly (string PlanId, string Name, int DurationMonths, int EmployeeLimit)[] HrPlans =
        {
            ("company-10", "Perusahaan 10 Karyawan (12 bln)", 12, 10),
            ("company-25", "Perusahaan 25 Karyawan (12 bln)", 12, 25),
            ("company-50", "Perusahaan 50 Karyawan (12 bln)", 12, 50),
        };

        // #Function GrantSubscription#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantSubscription(string userId, string role, string planId, int? psychologistId, int? companyId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var start = DateTime.Today;

            if (role == "Psychologist" && psychologistId.HasValue)
            {
                var plan = MitraPlans.FirstOrDefault(p => p.PlanId == planId);
                if (plan == default) return BadRequest("Plan tidak valid.");

                var psy = await _context.Psychologists.FindAsync(psychologistId.Value);
                if (psy == null) return NotFound();

                var old = await _context.PsychologistSubscriptions
                    .Where(s => s.PsychologistId == psychologistId.Value && s.Status == "Active")
                    .ToListAsync();
                foreach (var s in old) s.Status = "Expired";

                var end = start.AddMonths(plan.DurationMonths);
                _context.PsychologistSubscriptions.Add(new PsychologistSubscription
                {
                    PsychologistId = psychologistId.Value,
                    PlanName = plan.Name + " (Sponsor Admin)",
                    Status = "Active",
                    StartDate = start,
                    EndDate = end,
                    PatientLimit = plan.PatientLimit,
                });
                psy.IsMitraActive = true;
                await _context.SaveChangesAsync();
                TempData["success"] = $"Langganan {plan.Name} untuk {user.FullName} diaktifkan hingga {end:dd MMMM yyyy}.";
            }
            else if (role == "HR" && companyId.HasValue)
            {
                var plan = HrPlans.FirstOrDefault(p => p.PlanId == planId);
                if (plan == default) return BadRequest("Plan tidak valid.");

                var company = await _context.Companies.FindAsync(companyId.Value);
                if (company == null) return NotFound();

                var old = await _context.CompanySubscriptions
                    .Where(s => s.CompanyId == companyId.Value && s.Status == "Active")
                    .ToListAsync();
                foreach (var s in old) s.Status = "Expired";

                var end = start.AddMonths(plan.DurationMonths);
                _context.CompanySubscriptions.Add(new CompanySubscription
                {
                    CompanyId = companyId.Value,
                    PlanName = plan.Name + " (Sponsor Admin)",
                    Status = "Active",
                    StartDate = start,
                    EndDate = end,
                    EmployeeLimit = plan.EmployeeLimit,
                    MaxSessionsPerMonth = 4,
                });
                await _context.SaveChangesAsync();
                TempData["success"] = $"Langganan {plan.Name} untuk {user.FullName} ({company.Name}) diaktifkan hingga {end:dd MMMM yyyy}.";
            }

            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        // #Function RevokeSubscription#
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSubscription(string userId, string role, int? psychologistId, int? companyId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (role == "Psychologist" && psychologistId.HasValue)
            {
                var psy = await _context.Psychologists.FindAsync(psychologistId.Value);
                if (psy != null) psy.IsMitraActive = false;
                var active = await _context.PsychologistSubscriptions
                    .Where(s => s.PsychologistId == psychologistId.Value && s.Status == "Active")
                    .ToListAsync();
                foreach (var s in active) s.Status = "Expired";
                await _context.SaveChangesAsync();
                TempData["success"] = $"Langganan Mitra {user.FullName} dicabut.";
            }
            else if (role == "HR" && companyId.HasValue)
            {
                var active = await _context.CompanySubscriptions
                    .Where(s => s.CompanyId == companyId.Value && s.Status == "Active")
                    .ToListAsync();
                foreach (var s in active) s.Status = "Expired";
                await _context.SaveChangesAsync();
                TempData["success"] = $"Langganan HR dicabut.";
            }

            return RedirectToAction(nameof(Detail), new { id = userId });
        }

        // #Function Edit#


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

        // #Function Edit POST#

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

        // #Function Delete#

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Hapus data terkait sesuai role sebelum hapus user (semua FK pakai Restrict)
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == id);
            if (patient != null)
            {
                int pid = patient.PatientId;
                _context.Assignments.RemoveRange(_context.Assignments.Where(a => a.PatientId == pid));
                _context.Schedules.RemoveRange(_context.Schedules.Where(s => s.PatientId == pid));
                _context.Worksheets.RemoveRange(_context.Worksheets.Where(w => w.PatientId == pid));
                _context.MoodTrackers.RemoveRange(_context.MoodTrackers.Where(m => m.PatientId == pid));
                _context.Journals.RemoveRange(_context.Journals.Where(j => j.PatientId == pid));
                _context.JournalCheckIns.RemoveRange(_context.JournalCheckIns.Where(c => c.PatientId == pid));
                _context.Reports.RemoveRange(_context.Reports.Where(r => r.PatientId == pid));
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
            }

            var psychologist = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == id);
            if (psychologist != null)
            {
                int psyId = psychologist.PsychologistId;
                _context.Assignments.RemoveRange(_context.Assignments.Where(a => a.PsychologistId == psyId));
                _context.Psychologists.Remove(psychologist);
                await _context.SaveChangesAsync();
            }

            var hr = await _context.HrStaffs.FirstOrDefaultAsync(h => h.UserId == id);
            if (hr != null)
            {
                _context.HrStaffs.Remove(hr);
                await _context.SaveChangesAsync();
            }

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
