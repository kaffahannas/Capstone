using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // #Class Notification#
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RecipientUserId { get; set; } = "";
        [ForeignKey("RecipientUserId")]
        public virtual ApplicationUser? Recipient { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = "";

        [Required, StringLength(500)]
        public string Message { get; set; } = "";

        [StringLength(300)]
        public string? LinkUrl { get; set; }

        // Schedule, Worksheet, Assignment, Partnership, Cancellation, System
        [StringLength(32)]
        public string Type { get; set; } = "System";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
