using LightenUp.Web.Data;
using LightenUp.Web.Models;

namespace LightenUp.Web.Services
{
    public interface INotificationService
    {
        Task NotifyAsync(string? userId, string title, string message, string type, string? linkUrl = null);
    }

    // #Class NotificationService#
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // #Function NotifyAsync#
        public async Task NotifyAsync(string? userId, string title, string message, string type, string? linkUrl = null)
        {
            if (string.IsNullOrEmpty(userId)) return;

            _context.Notifications.Add(new Notification
            {
                RecipientUserId = userId,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
