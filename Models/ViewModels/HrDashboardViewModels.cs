namespace LightenUp.Web.Models.ViewModels
{
    public class HrDashboardViewModel
    {
        // ─── Personal / company context ───
        public string HrName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        // ─── Status counts ───
        public int ActiveCount { get; set; }
        public int SehatCount { get; set; }
        public int BeresikoCount { get; set; }
        public int BahayaCount { get; set; }

        public bool ShowBahayaBanner => BahayaCount > 0;

        public List<string> Divisions { get; set; } = new();   // for "Semua Divisi" dropdown

        // First 20 active clients for live-search dropdown
        public List<HrClientPreview> ClientsPreview { get; set; } = new();

        public List<HrPsyPreview> PartneredPsychologists { get; set; } = new();
        public string? PrimaryPsychologistEmail { get; set; }   // for fallback mailto
        public bool HasActiveSubscription { get; set; }

        // ─── Activity metrics ───
        public int TodaySessionCount { get; set; }
        public int WeekSessionCount { get; set; }
        public int ActiveWorksheetCount { get; set; }
        public int PendingRequestCount { get; set; }

        // ─── Recent activity feed ───
        public List<HrRecentActivity> RecentActivities { get; set; } = new();
    }

    public class HrClientPreview
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class HrRecentActivity
    {
        public string Type { get; set; } = string.Empty;          // "Worksheet" | "Session"
        public string PatientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    public class HrPsyPreview
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public int? Years { get; set; }
        public string? Email { get; set; }
        public bool IsPartner { get; set; }
    }
}
