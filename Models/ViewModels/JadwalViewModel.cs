using System;
using System.Collections.Generic;

namespace LightenUp.Web.Models.ViewModels
{
    // #Class JadwalItemViewModel#
    public class JadwalItemViewModel
    {
        public int ScheduleId { get; set; }
        public string PsychologistName { get; set; } = "";
        public DateTime SessionStart { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";
        public string? MeetingLink { get; set; }
    }

    // #Class JadwalViewModel#
    public class JadwalViewModel
    {
        public List<JadwalItemViewModel> UpcomingSessions { get; set; } = new();
        public List<JadwalItemViewModel> PastSessions { get; set; } = new();

        public bool HasActivePsychologist { get; set; }
        public string? PsychologistName { get; set; }
        public int? PsychologistId { get; set; }

        // Session quota info
        public int SessionsUsedThisMonth { get; set; }
        public int MaxSessionsPerMonth { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
    }
}
