using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.ViewComponents;

// #Class NotificationsViewComponent#
public class NotificationsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // #Function InvokeAsync#
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return View(new NotificationBellViewModel());

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
            return View(new NotificationBellViewModel());

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.RecipientUserId == user.Id && !n.IsRead);

        var recent = await _context.Notifications
            .Where(n => n.RecipientUserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .Select(n => new NotificationItem
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return View(new NotificationBellViewModel { UnreadCount = unreadCount, Recent = recent });
    }
}
