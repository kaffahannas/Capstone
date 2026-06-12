using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LightenUp.Web.Models;

namespace LightenUp.Web.Models.ViewModels
{
    public class PsychologistDashboardViewModel
    {
        public string PsychologistName { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public List<PatientListItem> Patients { get; set; } = new List<PatientListItem>();
        public List<PatientListItem> PendingAssignments { get; set; } = new List<PatientListItem>();
        public List<Company> PartnerCompanies { get; set; } = new List<Company>();
        public List<PatientListItem> UnassignedPatients { get; set; } = new List<PatientListItem>();
        public List<CompanyPsychologistRequest> PendingB2BRequests { get; set; } = new List<CompanyPsychologistRequest>();
    }

    public class PatientListItem
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Tambahan untuk Filter
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        // Assignment tracking
        public int AssignmentId { get; set; }
    }

    public class PatientDetailViewModel
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Age { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string JournalContent { get; set; } = string.Empty;
        public string Complaint { get; set; } = string.Empty;
    }

    public class StatisticsViewModel
    {
        public int TotalClients { get; set; }
        public int HealthyCount { get; set; }
        public int AtRiskCount { get; set; }
        public int DangerCount { get; set; }
    }

    public class WorksheetViewModel
    {
        public int TotalActivities { get; set; }
        public List<WorksheetItemViewModel> Tasks { get; set; } = new List<WorksheetItemViewModel>();
    }

    public class WorksheetItemViewModel
    {
        public int TaskId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }

    public class PsyScheduleEditViewModel
    {
        public int ScheduleId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        [Required] public DateTime SessionStart { get; set; }
        [Required] public int DurationMinutes { get; set; } = 60;
        [Required] public string Status { get; set; } = "Scheduled";
        public string? Notes { get; set; }
        public string? MeetingLink { get; set; }
    }

    public class WorksheetDetailViewModel
    {
        public int TaskId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string TaskDate { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string PsychologistNote { get; set; } = string.Empty;
    }

    public class PsychologistProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string LastDegree { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public string PracticeLocation { get; set; } = string.Empty;
        public string SiapNumber { get; set; } = string.Empty;
        public string SippNumber { get; set; } = string.Empty;
    }

    public class PayslipAssignmentRow
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientType { get; set; } = "B2C";
        public int CompletedSessions { get; set; }
        public int MaxSessions { get; set; }
        /// <summary>Monthly subscription value allocated to this patient (IDR).</summary>
        public decimal SubscriptionValue { get; set; }
        public decimal? B2BPlanAmount { get; set; }
        public int? B2BEmployeeCount { get; set; }
        public decimal PerSessionValue { get; set; }
        public decimal FeePercentage { get; set; }
        public decimal PsyShare { get; set; }
    }
    
    public class PayslipViewModel
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalGross { get; set; }
        public decimal TotalPsyShare { get; set; }
        public decimal TotalFeePercent { get; set; }
        public List<PayslipAssignmentRow> Assignments { get; set; } = new();
        public string PayoutStatus { get; set; } = "Pending";
    }


}
