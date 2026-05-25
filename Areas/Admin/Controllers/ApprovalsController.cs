using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _email;
        private readonly ILogger<ApprovalsController> _log;

        public ApprovalsController(ApplicationDbContext context,
                                   UserManager<ApplicationUser> userManager,
                                   IEmailSender email,
                                   ILogger<ApprovalsController> log)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "All")
        {
            var pending = await _userManager.Users
                .Where(u => !u.IsApprovedByAdmin && (u.RoleType == "Psychologist" || u.RoleType == "HR"))
                .ToListAsync();
            if (tab is "Psychologist" or "HR") pending = pending.Where(u => u.RoleType == tab).ToList();

            // Pre-fetch psy + hr + company lookups
            var psyMap = await _context.Psychologists
                .Where(p => pending.Select(u => u.Id).Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);
            var hrMap = await _context.HrStaffs
                .Include(h => h.Company)
                .Where(h => pending.Select(u => u.Id).Contains(h.UserId))
                .ToDictionaryAsync(h => h.UserId);

            var items = pending.Select(u =>
            {
                var item = new AdminApprovalItem
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Role = u.RoleType
                };
                if (u.RoleType == "Psychologist" && psyMap.TryGetValue(u.Id, out var p))
                {
                    item.LicenseNumber = p.LicenseNumber;
                    item.Specialization = p.Specialization;
                }
                else if (u.RoleType == "HR" && hrMap.TryGetValue(u.Id, out var h))
                {
                    item.CompanyName = h.Company?.Name;
                    item.SubmittedAt = h.OnboardingCompletedAt;
                }
                return item;
            }).OrderBy(i => i.SubmittedAt ?? DateTime.MinValue).ToList();

            ViewBag.ActiveNav = "Approvals";
            ViewData["Title"] = "Persetujuan";
            return View(new AdminApprovalsViewModel { Tab = tab, Items = items });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new AdminApprovalDetailViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                Role = user.RoleType,
                ProfilePicture = user.ProfilePicture,
                SubmittedAt = DateTime.Now
            };

            if (user.RoleType == "Psychologist")
            {
                var psy = await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (psy != null)
                {
                    vm.LicenseNumber = psy.LicenseNumber;
                    vm.SiapNumber = psy.SiapNumber;
                    vm.Specialization = psy.Specialization;
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
                    vm.CompanyAddress = hr.Company?.Address;
                    vm.CompanyRegistrationNumber = hr.Company?.RegistrationNumber;
                    vm.Department = hr.Department;
                    vm.SupportDocumentUrl = hr.SupportDocumentUrl;
                    vm.LastDegree = hr.LastDegree;
                    vm.University = hr.University;
                    vm.AcademicDocumentUrl = hr.AcademicDocumentUrl;
                    if (hr.OnboardingCompletedAt.HasValue) vm.SubmittedAt = hr.OnboardingCompletedAt.Value;
                }
            }

            ViewBag.ActiveNav = "Approvals";
            ViewData["Title"] = "Tinjau " + user.RoleType;
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(AdminApprovalActionViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            user.IsApprovedByAdmin = true;
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            await TrySendAsync(user.Email!,
                "Akun LightenUp Anda telah disetujui",
                $"Halo {user.FullName},\n\nSelamat! Akun {user.RoleType} Anda di LightenUp telah disetujui dan aktif. " +
                "Silakan login kembali untuk mengakses dashboard.\n\n" +
                $"Catatan dari Admin:\n{model.Note ?? "—"}\n\n— Tim LightenUp");

            TempData["success"] = $"{user.FullName} ({user.RoleType}) disetujui.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(AdminApprovalActionViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            user.IsActive = false;
            user.IsApprovedByAdmin = false;
            await _userManager.UpdateAsync(user);

            await TrySendAsync(user.Email!,
                "Pengajuan akun LightenUp Anda tidak disetujui",
                $"Halo {user.FullName},\n\nMohon maaf, pengajuan akun {user.RoleType} Anda saat ini belum dapat kami setujui.\n\n" +
                $"Catatan dari Admin:\n{model.Note ?? "—"}\n\n" +
                "Anda dapat menghubungi support@lightenup.com untuk informasi lebih lanjut.\n\n— Tim LightenUp");

            TempData["success"] = $"{user.FullName} ditolak. Akun dinonaktifkan.";
            return RedirectToAction(nameof(Index));
        }

        private async Task TrySendAsync(string to, string subject, string body)
        {
            try { await _email.SendAsync(to, subject, body); }
            catch (Exception ex) { _log.LogWarning(ex, "Failed to send approval email to {To}", to); }
        }
    }
}
