namespace LightenUp.Web.Models.ViewModels
{
    // #Class NotificationBellViewModel#
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItem> Recent { get; set; } = new();
    }

    // #Class NotificationItem#
    public class NotificationItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? LinkUrl { get; set; }
        public string Type { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
