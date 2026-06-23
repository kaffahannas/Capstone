using LightenUp.Web.Data;
using LightenUp.Web.Filters;
using LightenUp.Web.Models;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Areas.Psychologist.Controllers
{
    [Area("Psychologist")]
    [Authorize(Roles = "Psychologist")]
    public class MitraController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionAccessService _access;

        public MitraController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SubscriptionAccessService access)
        {
            _context = context;
            _userManager = userManager;
            _access = access;
        }

        private async Task<LightenUp.Web.Models.Psychologist?> GetPsyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        // ─── Halaman utama Mitra — status langganan + kode referal ───
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return RedirectToAction("Login", "Account", new { area = "" });

            var activeSub = await _access.GetActivePsychologistSubscriptionAsync(psy.PsychologistId);
            var clientCount = await _context.Patients
                .CountAsync(p => p.SponsorPsychologistId == psy.PsychologistId);

            ViewBag.Psy = psy;
            ViewBag.ActiveSub = activeSub;
            ViewBag.ClientCount = clientCount;
            ViewBag.IsMitraActive = activeSub != null;
            ViewBag.ActiveNav = "Mitra";
            ViewData["Title"] = "Mitra LightenUp";

            if (TempData["mitraRequired"] != null)
                TempData["info"] = "Aktifkan add-on Mitra untuk mengakses fitur monitoring klien klinik.";

            return View();
        }

        // ─── Generate kode referal baru ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiresPsychologistMitra]
        public async Task<IActionResult> GenerateReferralCode()
        {
            var psy = await GetPsyAsync();
            if (psy == null) return NotFound();

            psy.MitraReferralCode = await _access.GenerateUniqueReferralCodeAsync();
            await _context.SaveChangesAsync();

            TempData["success"] = $"Kode referal baru: {psy.MitraReferralCode}";
            return RedirectToAction(nameof(Index));
        }
    }
}
