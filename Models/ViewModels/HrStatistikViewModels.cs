namespace LightenUp.Web.Models.ViewModels
{
    public class HrStatistikOverviewViewModel
    {
        public string CompanyName { get; set; } = "";
        public int ActiveCount { get; set; }
        public int Sehat { get; set; }
        public int Beresiko { get; set; }
        public int Bahaya { get; set; }

        // Slice 4 spec — % Beresiko + Bahaya per division
        public List<DivisionStress> Divisions { get; set; } = new();
    }

    public class DivisionStress
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }                 // # of employees in division
        public int StressPct { get; set; }             // % Beresiko + Bahaya
        public int SehatPct { get; set; }
        public int BeresikoPct { get; set; }
        public int BahayaPct { get; set; }
        public int SehatCount { get; set; }
        public int BeresikoCount { get; set; }
        public int BahayaCount { get; set; }
    }

    public class HrDivisionReportViewModel
    {
        public string DivisionName { get; set; } = "";
        public int Window { get; set; } = 30;          // 7 / 30 / 90 days

        // Mood trend (avg per day across all employees in division)
        public List<DateTime> Dates { get; set; } = new();
        public List<double> AvgScores { get; set; } = new();

        // Status donut
        public int Sehat { get; set; }
        public int Beresiko { get; set; }
        public int Bahaya { get; set; }
        public int Total => Sehat + Beresiko + Bahaya;
    }

    // Roster row for CSV/print export
    public class HrRosterRow
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Division { get; set; }
        public string? EmployeeId { get; set; }
        public string MentalHealthStatus { get; set; } = "";
        public string LastCheckDate { get; set; } = "";
        public int? TotalMoodPercent { get; set; }
    }
}
