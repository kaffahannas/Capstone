using System.ComponentModel.DataAnnotations;
using LightenUp.Web.Services;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── Dashboard ───
    // #Class AdminDashboardViewModel#
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPatients { get; set; }
        public int TotalPsychologists { get; set; }
        public int TotalHrs { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalCompanies { get; set; }
        public int PendingPsychologists { get; set; }
        public int PendingHrs { get; set; }
        public int PendingTotal => PendingPsychologists + PendingHrs;

        public int PendingAdminAssignments { get; set; }
        public int PendingCancellationByAdmin { get; set; }
        public int PendingPatientAdminRequests { get; set; }
        public int PendingAssignmentsTotal => PendingAdminAssignments + PendingCancellationByAdmin + PendingPatientAdminRequests;
    }

    // ─── Approvals queue ───
    // #Class AdminApprovalItem#
    public class AdminApprovalItem
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";          // Psychologist / HR
        public DateTime? SubmittedAt { get; set; }
        public string? CompanyName { get; set; }         // For HR
        public string? LicenseNumber { get; set; }       // For Psychologist
        public string? Specialization { get; set; }      // For Psychologist
    }

    // #Class AdminApprovalsViewModel#
    public class AdminApprovalsViewModel
    {
        public string Tab { get; set; } = "All";   // All / Psychologist / HR
        public List<AdminApprovalItem> Items { get; set; } = new();
    }

    // #Class AdminApprovalDetailViewModel#
    public class AdminApprovalDetailViewModel
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }

        // Psychologist details
        public string? LicenseNumber { get; set; }
        public string? SiapNumber { get; set; }
        public string? Specialization { get; set; }
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public int? ExperienceYears { get; set; }
        public string? PracticeLocation { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? StrDocumentUrl { get; set; }

        // HR details
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyRegistrationNumber { get; set; }
        public string? Department { get; set; }
        public string? SupportDocumentUrl { get; set; }

        public DateTime SubmittedAt { get; set; }
    }

    // #Class AdminApprovalActionViewModel#
    public class AdminApprovalActionViewModel
    {
        public string UserId { get; set; } = "";
        [StringLength(500)]
        public string? Note { get; set; }
    }

    // ─── Users ───
    // #Class AdminUserItem#
    public class AdminUserItem
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsApprovedByAdmin { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // #Class AdminUsersListViewModel#
    public class AdminUsersListViewModel
    {
        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }       // Active / Inactive / Pending
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public List<AdminUserItem> Items { get; set; } = new();
    }

    // #Class AdminUserDetailViewModel#
    public class AdminUserDetailViewModel
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsApprovedByAdmin { get; set; }
        public string? ProfilePicture { get; set; }

        // Role-specific
        public string? CompanyName { get; set; }
        public string? Department { get; set; }
        public string? MentalHealthStatus { get; set; }
        
        // HR specific
        public string? CompanyAddress { get; set; }
        public string? CompanyRegistrationNumber { get; set; }
        public string? SupportDocumentUrl { get; set; }

        // Psychologist specific
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public string? SiapNumber { get; set; }
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public int? ExperienceYears { get; set; }
        public string? PracticeLocation { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? StrDocumentUrl { get; set; }

        // Patient specific
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    // #Class AdminUserEditViewModel#
    public class AdminUserEditViewModel
    {
        [Required]
        public string UserId { get; set; } = "";
        
        [Required, StringLength(100)]
        public string FullName { get; set; } = "";
        
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        
        public string? Phone { get; set; }
        
        [Required]
        public string Role { get; set; } = ""; // "Admin", "HR", "Patient", "Psychologist"
    }

    // ─── Companies ───
    // #Class AdminCompanyItem#
    public class AdminCompanyItem
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public int DivisionCount { get; set; }
        public string? HrName { get; set; }
        public int PatientCount { get; set; }
        public int ActivePatientCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // #Class AdminCompaniesListViewModel#
    public class AdminCompaniesListViewModel
    {
        public string? Search { get; set; }
        public List<AdminCompanyItem> Items { get; set; } = new();
    }

    // #Class AdminCompanyDetailViewModel#
    public class AdminCompanyDetailViewModel
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? RegistrationNumber { get; set; }
        public int DivisionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AdminUserItem> Hrs { get; set; } = new();
        public int TotalPatients { get; set; }
        public int SehatCount { get; set; }
        public int BeresikoCount { get; set; }
        public int BahayaCount { get; set; }
    }

    // ─── Settings ───
    // #Class AdminSettingsViewModel#
    public class AdminSettingsViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
    }

    // #Class AdminChangePasswordViewModel#
    public class AdminChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required, Compare("NewPassword"), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }

    // #Class AdminNavBadgesViewModel#
    public class AdminNavBadgesViewModel
    {
        public int Count { get; set; }
    }

    // #Class AdminAssignmentsIndexViewModel#
    public class AdminAssignmentsIndexViewModel
    {
        public string Tab { get; set; } = "psy";
        public List<PatientPsychologistAssignment> PendingAssignments { get; set; } = new();
        public List<PatientAdminAssignmentRequest> PatientAdminRequests { get; set; } = new();
        public List<PsychologistWorkloadInfo> Psychologists { get; set; } = new();
        public List<CompanyPsychologistRequest> B2BRequests { get; set; } = new();
        public int PendingPsyCount { get; set; }
        public int PendingCancelCount { get; set; }
        public int PendingPatientRequestCount { get; set; }
        public int PendingB2BRequestCount { get; set; }
    }
}
