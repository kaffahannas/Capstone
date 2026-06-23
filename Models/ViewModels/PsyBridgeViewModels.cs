using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── PsychologistRequest inbox ───
    // #Class PsyRequestListItem#
    public class PsyRequestListItem
    {
        public int Id { get; set; }
        /// <summary>Set when <see cref="RequestType"/> is Assignment — use for Accept/Reject actions.</summary>
        public int? AssignmentId { get; set; }
        public string RequestType { get; set; } = "";   // Worksheet / Schedule / Assignment
        public string PatientName { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string HrName { get; set; } = "";
        public string? Notes { get; set; }
        public string? ProposedTaskName { get; set; }
        public DateTime? ProposedDeadline { get; set; }
        public DateTime? ProposedSessionDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected
        public DateTime CreatedAt { get; set; }
        public string? RespondedNote { get; set; }
    }

    // #Class PsyRequestsViewModel#
    public class PsyRequestsViewModel
    {
        public string Tab { get; set; } = "Pending";
        public List<PsyRequestListItem> Items { get; set; } = new();
    }

    // #Class PsyNavBadgesViewModel#
    public class PsyNavBadgesViewModel
    {
        public int Count { get; set; }
    }

    // #Class PsyRespondViewModel#
    public class PsyRespondViewModel
    {
        public int Id { get; set; }
        [Required]
        public string Action { get; set; } = "";   // "Approve" or "Reject"
        [StringLength(500)]
        public string? Note { get; set; }
    }

    // ─── Worksheet review (psy approves a worksheet) ───
    // #Class PsyWorksheetReviewViewModel#
    public class PsyWorksheetReviewViewModel
    {
        public int WorksheetId { get; set; }
        public string PatientName { get; set; } = "";
        public string TaskName { get; set; } = "";
        public string? Description { get; set; }
        public string? ProofImagePath { get; set; }
        public string? PatientNote { get; set; }
        public string Status { get; set; } = "";

        [StringLength(1000)]
        public string? PsychologistFeedback { get; set; }
    }

    // ─── Settings (AcceptsB2B toggle) ───
    // #Class PsySettingsViewModel#
    public class PsySettingsViewModel
    {
        public bool AcceptsB2B { get; set; }
    }
}
