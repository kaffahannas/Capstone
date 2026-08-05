using LightenUp.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Controllers
{
    // #Class NotificationsController#
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // #Function Open#
        // Marks a notification read and redirects to its link (or back home if none).
        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == user.Id);
            if (notif == null) return RedirectToAction("Index", "Home", new { area = "" });

            if (!notif.IsRead)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(notif.LinkUrl))
                return Redirect(notif.LinkUrl);

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // #Function MarkAllRead#
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var unread = await _context.Notifications
                .Where(n => n.RecipientUserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
