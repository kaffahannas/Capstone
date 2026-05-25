using System;
using System.Collections.Generic;

namespace LightenUp.Web.Models.ViewModels
{
    public class JadwalItemViewModel
    {
        public int ScheduleId { get; set; }
        public string PsychologistName { get; set; } = "";
        public DateTime SessionStart { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = "";
        public string? MeetingLink { get; set; }
    }

    public class JadwalViewModel
    {
        public List<JadwalItemViewModel> UpcomingSessions { get; set; } = new();
        public List<JadwalItemViewModel> PastSessions { get; set; } = new();
    }
}
