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
        private readonly AssignmentActivationService _activation;

        public ApprovalsController(ApplicationDbContext context,
                                   UserManager<ApplicationUser> userManager,
                                   IEmailSender email,
                                   ILogger<ApprovalsController> log,
                                   AssignmentActivationService activation)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
            _log = log;
            _activation = activation;
        }

        private async Task SetNavCountsAsync()
        {
            ViewBag.CountAccounts = await _userManager.Users.CountAsync(u => !u.IsApprovedByAdmin && u.RoleType == "Psychologist");
            ViewBag.CountRemovals = await _context.HrEmployeeRemovalRequests.CountAsync(r => r.Status == "Pending");
            ViewBag.CountAssignments = await _context.Assignments.CountAsync(a => a.Status == "PendingAdminApproval");
            ViewBag.CountCancellations = await _context.Assignments.CountAsync(a => a.Status == "PendingCancellationByAdmin");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "All")
        {
            await SetNavCountsAsync();
            // HR is now auto-approved at registration; only Psychologists need manual approval.
            var pending = await _userManager.Users
                .Where(u => !u.IsApprovedByAdmin && u.RoleType == "Psychologist")
                .ToListAsync();
            if (tab == "Psychologist") pending = pending.Where(u => u.RoleType == tab).ToList();

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
                    item.Specialization = p.SiapNumber; // Overriding Specialization to carry SiapNumber to UI
                    item.SubmittedAt = p.OnboardingCompletedAt;
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
                SubmittedAt = DateTime.UtcNow
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

        // --- EMPLOYEE REMOVAL REQUESTS ---
        [HttpGet]
        public async Task<IActionResult> EmployeeRemovals()
        {
            await SetNavCountsAsync();
            var requests = await _context.HrEmployeeRemovalRequests
                .Include(r => r.Patient!)
                    .ThenInclude(p => p.User)
                .Include(r => r.RequestedByHr)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveNav = "Approvals";
            ViewData["Title"] = "Pemberhentian Karyawan";
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveEmployeeRemoval(int id, string note)
        {
            var req = await _context.HrEmployeeRemovalRequests.Include(r => r.Patient).FirstOrDefaultAsync(r => r.Id == id);
            if (req == null) return NotFound();

            var adminId = _userManager.GetUserId(User);

            req.Status = "Approved";
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionByAdminUserId = adminId;
            req.DecisionNote = note;

            if (req.Patient != null)
            {
                var companyId = req.Patient.CompanyId;
                req.Patient.CompanyId = null;

                if (companyId.HasValue)
                {
                    var assignments = await _context.Assignments
                        .Where(a => a.PatientId == req.PatientId && a.Status == "Active")
                        .ToListAsync();
                    foreach (var a in assignments)
                    {
                        a.Status = "CancelledByAdmin";
                        a.CancellationReason = "Karyawan diberhentikan oleh HR.";
                        a.CancellationRequestedAt = DateTime.UtcNow;
                        a.CancellationRequestedByUserId = adminId;
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pemberhentian disetujui. Akun karyawan menjadi publik.";
            return RedirectToAction(nameof(EmployeeRemovals));
        }

        [HttpPost]
        public async Task<IActionResult> RejectEmployeeRemoval(int id, string note)
        {
            var req = await _context.HrEmployeeRemovalRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null) return NotFound();

            var adminId = _userManager.GetUserId(User);

            req.Status = "Rejected";
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionByAdminUserId = adminId;
            req.DecisionNote = note;

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan pemberhentian ditolak.";
            return RedirectToAction(nameof(EmployeeRemovals));
        }

        // --- ASSIGNMENT & CANCELLATION REQUESTS ---
        [HttpGet]
        public async Task<IActionResult> AssignmentRequests()
        {
            await SetNavCountsAsync();
            var pendingAssignments = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .Include(a => a.RequestedBy)
                .Where(a => a.Status == "PendingAdminApproval")
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            ViewBag.ActiveNav = "Approvals";
            ViewData["Title"] = "Persetujuan Penugasan";
            return View(pendingAssignments);
        }

        [HttpGet]
        public async Task<IActionResult> CancellationRequests()
        {
            await SetNavCountsAsync();
            var pendingCancellations = await _context.Assignments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Patient).ThenInclude(p => p!.Company)
                .Include(a => a.Psychologist).ThenInclude(p => p!.User)
                .Include(a => a.CancellationRequestedBy)
                .Where(a => a.Status == "PendingCancellationByAdmin")
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            ViewBag.ActiveNav = "Approvals";
            ViewData["Title"] = "Persetujuan Pembatalan HR";
            return View(pendingCancellations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAssignment(int assignmentId, string? note, decimal? psychologistRevenuePercentage)
        {
            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments.FindAsync(assignmentId);
            if (a == null) return NotFound();

            var returnAction = a.Status == "PendingCancellationByAdmin" ? nameof(CancellationRequests) : nameof(AssignmentRequests);

            if (a.Status == "PendingAdminApproval")
            {
                await _activation.ActivateAsync(a, user?.Id, note, psychologistRevenuePercentage);
            }
            else if (a.Status == "PendingCancellationByAdmin")
            {
                a.Status = "Cancelled";
                a.DecisionByUserId = user?.Id;
                a.DecisionAt = DateTime.UtcNow;
                a.DecisionNote = note;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan disetujui.";
            return RedirectToAction(returnAction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAssignment(int assignmentId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            var a = await _context.Assignments.FindAsync(assignmentId);
            if (a == null) return NotFound();

            var returnAction = a.Status == "PendingCancellationByAdmin" ? nameof(CancellationRequests) : nameof(AssignmentRequests);

            if (a.Status == "PendingCancellationByAdmin")
                a.Status = "Active";
            else
                a.Status = "Rejected";

            a.DecisionByUserId = user?.Id;
            a.DecisionAt = DateTime.UtcNow;
            a.DecisionNote = note;

            await _context.SaveChangesAsync();
            TempData["success"] = "Permintaan ditolak.";
            return RedirectToAction(returnAction);
        }
    }
}
