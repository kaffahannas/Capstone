using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace LightenUp.Web.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class JadwalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JadwalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveNav = "Jadwal";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Schedules)
                    .ThenInclude(s => s.Psychologist)
                        .ThenInclude(psy => psy.User)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (patient == null) return NotFound("Patient record not found.");

            var vm = new JadwalViewModel();

            var allSchedules = patient.Schedules
                .Select(s => new JadwalItemViewModel
                {
                    ScheduleId = s.ScheduleId,
                    PsychologistName = s.Psychologist?.User?.FullName ?? "Dr. Unknown",
                    SessionStart = s.SessionStart,
                    DurationMinutes = s.DurationMinutes,
                    Status = s.Status,
                    MeetingLink = s.MeetingLink
                })
                .OrderBy(s => s.SessionStart)
                .ToList();

            var today = DateTime.Now;

            vm.UpcomingSessions = allSchedules.Where(s => s.SessionStart >= today || s.Status == "Scheduled").ToList();
            vm.PastSessions = allSchedules.Where(s => s.SessionStart < today && s.Status != "Scheduled").OrderByDescending(s => s.SessionStart).ToList();

            return View(vm);
        }
    }
}
