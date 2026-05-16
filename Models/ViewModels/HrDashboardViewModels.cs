namespace LightenUp.Web.Models.ViewModels
{
    public class HrDashboardViewModel
    {
        public int ActiveCount { get; set; }
        public int SehatCount { get; set; }
        public int BeresikoCount { get; set; }
        public int BahayaCount { get; set; }

        public bool ShowBahayaBanner => BahayaCount > 0;

        public List<string> Divisions { get; set; } = new();   // for "Semua Divisi" dropdown

        // First 20 active clients for live-search dropdown
        public List<HrClientPreview> ClientsPreview { get; set; } = new();

        public string? PrimaryPsychologistEmail { get; set; }   // for "Kontak Psikolog" mailto
        public string? CompanyReferralCode { get; set; }
    }

    public class HrClientPreview
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
