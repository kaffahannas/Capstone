using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LightenUp.Web.Models.ViewModels
{
    public class TaskListItemViewModel
    {
        public int WorksheetId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;     // Assigned/InProgress/Completed
        public string StatusLabel { get; set; } = string.Empty; // Belum Dikerjakan / Sedang Dikerjakan / Selesai
        public string PsychologistName { get; set; } = string.Empty;
        public string DateLabel { get; set; } = string.Empty;   // "3 bulan" or "19 Oktober 2025"
        public DateTime Deadline { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class TaskListViewModel
    {
        public string? Search { get; set; }
        public List<string> Statuses { get; set; } = new();   // Selected status filter chips
        public List<string> Periods { get; set; } = new();    // Selected period filter chips
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public List<TaskListItemViewModel> Items { get; set; } = new();
        public bool HasMore => Page * PageSize < TotalCount;
    }

    public class TaskDetailViewModel
    {
        public int WorksheetId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string PsychologistName { get; set; } = string.Empty;

        public string? ProofImagePath { get; set; }
        public string? Note { get; set; }
        public string? PsychologistFeedback { get; set; }

        public bool IsLocked => Status == "Completed";
    }

    public class TaskSubmitViewModel
    {
        public int WorksheetId { get; set; }

        public IFormFile? Photo { get; set; }

        [StringLength(2000)]
        public string? Note { get; set; }
    }

    // ─── Status helpers ───
    public static class WorksheetStatus
    {
        public const string Assigned = "Assigned";
        public const string InProgress = "InProgress";
        public const string NeedsRevision = "NeedsRevision";
        public const string Completed = "Completed";

        public static string Label(string status) => status switch
        {
            Assigned => "Belum Dikerjakan",
            InProgress => "Sedang Dikerjakan",
            NeedsRevision => "Perlu Revisi",
            Completed => "Selesai",
            _ => status
        };

        public static string CssClass(string status) => status switch
        {
            Assigned => "task-status-todo",
            InProgress => "task-status-progress",
            NeedsRevision => "task-status-todo",
            Completed => "task-status-done",
            _ => ""
        };
    }
}
