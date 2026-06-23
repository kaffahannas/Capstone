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
    // #Class AssignmentsController#
    [Authorize(Roles = "Admin")]
    public class AssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AssignmentActivationService _activation;
        private readonly PsychologistWorkloadService _workload;
        private readonly SubscriptionPricingService _pricing;

        public AssignmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AssignmentActivationService activation,
            PsychologistWorkloadService workload,
            SubscriptionPricingService pricing)
        {
            _context = context;
            _userManager = userManager;
            _activation = activation;
            _workload = workload;
            _pricing = pricing;
        }

        // #Bagian Daftar Penugasan#
        // #Function Index#

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "b2b")
        {
            var pendingB2BCount = await _context.CompanyPsychologistRequests.CountAsync(r => r.PsychologistId == null && r.Status == "Pending");

            var b2bRequests = new List<CompanyPsychologistRequest>();
            List<PsychologistWorkloadInfo> psychologists = new();
            
            if (tab == "workload")
            {
                psychologists = await _workload.GetApprovedPsychologistsWithWorkloadAsync(b2bOnly: false);
            }
            else if (tab == "b2b")
            {
                b2bRequests = await _context.CompanyPsychologistRequests
                    .Include(r => r.Company)
                    .Where(r => r.PsychologistId == null && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                psychologists = await _workload.GetApprovedPsychologistsWithWorkloadAsync(b2bOnly: true);
            }
            else 
            {
                // Fallback tab
                tab = "b2b";
            }

            ViewBag.ActiveNav = "Assignments";
            ViewData["Title"] = "Penugasan Psikolog";
            return View(new AdminAssignmentsIndexViewModel
            {
                Tab = tab,
                B2BRequests = b2bRequests,
                Psychologists = psychologists,
                PendingB2BRequestCount = pendingB2BCount
            });
        }


        // #Bagian Penugasan B2B#
        // #Function AssignB2BPsychologist#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignB2BPsychologist(int requestId, int psychologistId)
        {
            var req = await _context.CompanyPsychologistRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == null && r.Status == "Pending");
            if (req == null) return NotFound();

            req.PsychologistId = psychologistId;
            req.Status = "Approved";
            req.RespondedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan kemitraan telah diteruskan ke psikolog terpilih.";
            return RedirectToAction(nameof(Index), new { tab = "b2b" });
        }

        // #Function DismissB2BRequest#

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissB2BRequest(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.CompanyPsychologistRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.PsychologistId == null && r.Status == "Pending");
            if (req == null) return NotFound();

            req.Status = "Rejected"; // Or "Dismissed" if we add that status for B2B requests. The model comment says Pending, Approved, Rejected.
            req.RespondedDate = DateTime.UtcNow;
            req.HandledByAdminUserId = user?.Id;
            
            await _context.SaveChangesAsync();

            TempData["success"] = "Permintaan B2B ditolak / ditutup.";
            return RedirectToAction(nameof(Index), new { tab = "b2b" });
        }
    }
}
