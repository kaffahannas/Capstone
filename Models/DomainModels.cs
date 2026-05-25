using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LightenUp.Web.Models
{
    // ==========================================
    // 1. MODEL YANG DIMODIFIKASI
    // ==========================================

    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // --- B2B/B2C ---
        // Null = Pasien Publik (B2C). Ada isi = Karyawan (B2B).
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public string? EmployeeId { get; set; }
        public string? Department { get; set; }                 // B2B only — "Divisi Kreatif"
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }                     // Male / Female
        public string EmploymentStatus { get; set; } = "active";

        // --- Mental-health status (computed from MoodTracker + JournalCheckIn) ---
        public string MentalHealthStatus { get; set; } = "Sehat"; // Sehat, Beresiko, Bahaya

        // Free-text symptoms/catatan shown on HR employee detail page (slice 3)
        public string? Symptoms { get; set; }

        // --- 14-step onboarding survey answers ---
        public string? RelationshipStatus { get; set; }         // Single, Dating, Married, Divorced, PreferNotToSay
        public string? SpiritualActivity { get; set; }          // Active, Rare, Inactive
        public bool? HasPreviousCounseling { get; set; }
        public string? CounselingMethods { get; set; }          // CSV: "CBT,Hypnotherapy,..."
        public string? CounselingMethodOther { get; set; }      // free text for "Lainnya"
        public bool? HasMedicationHistory { get; set; }
        public string? SleepQuality { get; set; }               // VeryGood, Average, Poor, Bad, VeryBad
        public string? AppGoals { get; set; }                   // CSV: "MoodTracking,Journaling,PersonalGoals,Unsure"
        public DateTime? TermsAcceptedAt { get; set; }
        public DateTime? OnboardingCompletedAt { get; set; }    // null until step 16 confirmed

        // --- Emergency contact (Profile page) ---
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactEmail { get; set; }
        public string? EmergencyContactRelation { get; set; }   // "Pasangan", "Saudara", etc.

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public virtual ICollection<PatientPsychologistAssignment> Assignments { get; set; } = new List<PatientPsychologistAssignment>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<Worksheet> Worksheets { get; set; } = new List<Worksheet>();
        public virtual ICollection<MoodTracker> MoodLogs { get; set; } = new List<MoodTracker>();
        public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();
        public virtual ICollection<JournalCheckIn> JournalCheckIns { get; set; } = new List<JournalCheckIn>();
        public virtual PatientNotificationPreference? NotificationPreference { get; set; }
    }

    public class Psychologist
    {
        [Key]
        public int PsychologistId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Bio { get; set; }

        // Data Onboarding (Asli milik Anda)
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? SiapNumber { get; set; }
        public string? StrDocumentUrl { get; set; }
        public string? PracticeLocation { get; set; }

        // --- NAVIGATION PROPERTIES (RELASI) ---
        public virtual ICollection<PatientPsychologistAssignment> Assignments { get; set; } = new List<PatientPsychologistAssignment>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<Worksheet> Worksheets { get; set; } = new List<Worksheet>();

        // Many-to-Many to Perusahaan
        public virtual ICollection<Company> PartneredCompanies { get; set; } = new List<Company>();

        // Opt-in to be visible in HR company directory (slice 6)
        public bool AcceptsB2B { get; set; } = false;

        // ─── Psychologist profile additions (Psy slice 10) ───
        public DateTime? OnboardingCompletedAt { get; set; }
        public string? AvailabilityText { get; set; }      // "Mon-Fri: 9AM-5PM"
        public bool IsAvailable { get; set; } = true;
        public string? OfficeAddress { get; set; }

        // ─── 1-to-1 notification preferences (Psy slice 10) ───
        public virtual PsyNotificationPreference? NotificationPreference { get; set; }
    }


    public class HrStaff
    {
        [Key]
        public int HrId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // HR pasti memiliki Perusahaan
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public string? Department { get; set; }       // Division this HR manages
        public string? EmployeeId { get; set; }       // HR's own ID at the company (e.g. "HR001")

        // ─── Onboarding survey ───
        public string? LastDegree { get; set; }
        public string? University { get; set; }
        public string? AcademicDocumentUrl { get; set; }
        public string? SupportDocumentUrl { get; set; }    // company supporting doc (e.g. SIPP scan)
        public DateTime? OnboardingCompletedAt { get; set; }

        // ─── Navigation ───
        public virtual HrNotificationPreference? NotificationPreference { get; set; }
    }

    // ==========================================
    // 2. KELAS ENTITAS BARU (DITAMBAHKAN KE SINI)
    // ==========================================

    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? ContactNumber { get; set; }
        public string? ContactEmail { get; set; }               // For "Kontak HRD" mailto: link on Patient profile
        public string? RegistrationNumber { get; set; }         // "Nomor Perusahaan" — free-form business registration

        public string? ReferralCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<HrStaff> HrStaffs { get; set; } = new List<HrStaff>();
        public virtual ICollection<CompanySubscription> Subscriptions { get; set; } = new List<CompanySubscription>();
        public virtual ICollection<CompanyDivision> Divisions { get; set; } = new List<CompanyDivision>();

        // Many-to-Many to Psychologist
        public virtual ICollection<Psychologist> PartneredPsychologists { get; set; } = new List<Psychologist>();
    }

    public class CompanyDivision
    {
        [Key]
        public int DivisionId { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The unique referral code for this specific division
        public string? ReferralCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>B2B company plan purchased by HR; activates referral code for employees.</summary>
    public class CompanySubscription
    {
        [Key]
        public int CompanySubscriptionId { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }

    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual ICollection<PaymentTransaction> Payments { get; set; } = new List<PaymentTransaction>();
    }

    /// <summary>DuitKu payment attempt (B2C patient or B2B company subscription).</summary>
    public class PaymentTransaction
    {
        [Key]
        public int PaymentId { get; set; }

        public int? PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public int? SubscriptionId { get; set; }
        [ForeignKey("SubscriptionId")]
        public virtual Subscription? Subscription { get; set; }

        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public int? CompanySubscriptionId { get; set; }
        [ForeignKey("CompanySubscriptionId")]
        public virtual CompanySubscription? CompanySubscription { get; set; }

        [Required, MaxLength(100)]
        public string MerchantOrderId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DuitkuReference { get; set; }

        [Column(TypeName = "decimal(14,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        /// <summary>pending | paid | failed | expired</summary>
        [Required, MaxLength(32)]
        public string PaymentStatus { get; set; } = "pending";

        [MaxLength(10)]
        public string? ResultCode { get; set; }

        [MaxLength(255)]
        public string? ResultMessage { get; set; }

        public string? CallbackPayload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }

    public class PatientPsychologistAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        // Jika B2C (Publik) = NULL. Jika B2B (Perusahaan) = ID User HR.
        // Now a real FK to AspNetUsers (configured in OnModelCreating).
        public string? AssignedByHrUserId { get; set; }
        [ForeignKey("AssignedByHrUserId")]
        public virtual ApplicationUser? AssignedByHr { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active";
    }

    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        // Combined date + time for the session start (was: SessionDate + SessionTime).
        // One column makes range queries (e.g. "sessions between 9am and 5pm Tuesday") simple.
        public DateTime SessionStart { get; set; }

        // Optional duration; defaults to 60 minutes.
        public int DurationMinutes { get; set; } = 60;

        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled

        public string? MeetingLink { get; set; }
        public string? Notes { get; set; }
    }

    public class Worksheet
    {
        [Key]
        public int WorksheetId { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime Deadline { get; set; }

        // Assigned (Belum Dikerjakan) → InProgress (Sedang Dikerjakan) → Completed (Selesai)
        public string Status { get; set; } = "Assigned";

        public string? ProofImagePath { get; set; }     // Patient's uploaded photo
        public string? Note { get; set; }               // Patient's "Deskripsikan Perasaanmu"
        public string? PsychologistFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? SubmittedAt { get; set; }      // Set when patient first submits proof
        public DateTime? ReviewedAt { get; set; }       // Set when psychologist marks Completed

        public string? HrNote { get; set; }             // HR's note from /Hr/Worksheets/Review (slice 5)
    }

    // ─── HR slice 2: pre-registered employees ─────
    // HR adds expected employees here; when a patient registers with the company's referral code
    // and matching email, the row gets claimed and the patient's Dept/EmployeeId auto-fill.
    public class PendingEmployee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(64)] public string? Department { get; set; }
        [StringLength(64)] public string? EmployeeId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? ClaimedByPatientId { get; set; }
        [ForeignKey("ClaimedByPatientId")]
        public virtual Patient? ClaimedByPatient { get; set; }

        public DateTime? ClaimedAt { get; set; }
    }

    // ─── HR slice 5: requests from HR to psychologist ─────
    // "+" buttons on /Hr/Worksheets and /Hr/Schedules create these.
    public class PsychologistRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RequestedByHrUserId { get; set; } = string.Empty;
        [ForeignKey("RequestedByHrUserId")]
        public virtual ApplicationUser? RequestedByHr { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public int? PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [Required, StringLength(32)]
        public string RequestType { get; set; } = string.Empty;     // "Worksheet" or "Schedule"

        [StringLength(1000)] public string? Notes { get; set; }
        [StringLength(200)]  public string? ProposedTaskName { get; set; }
        public DateTime? ProposedDeadline { get; set; }
        public DateTime? ProposedSessionDate { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = "Pending";  // Pending / Approved / Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
        [StringLength(500)] public string? RespondedNote { get; set; }
    }

    // ─── HR slice 6 & Psy slice 9: escalation reports (bidirectional) ─────
    public class Report
    {
        [Key]
        public int Id { get; set; }

        // "HrToPsy" (default — HR escalates to psychologist)
        // "PsyToHr" (psychologist reports patient back to HR)
        [Required, StringLength(16)]
        public string Direction { get; set; } = "HrToPsy";

        // For HrToPsy: this is the sending HR. For PsyToHr: leave null (use ReportedByPsyUserId instead).
        public string? ReportedByHrUserId { get; set; }
        [ForeignKey("ReportedByHrUserId")]
        public virtual ApplicationUser? ReportedByHr { get; set; }

        // For PsyToHr: this is the sending psy. For HrToPsy: leave null.
        public string? ReportedByPsyUserId { get; set; }
        [ForeignKey("ReportedByPsyUserId")]
        public virtual ApplicationUser? ReportedByPsy { get; set; }

        // For PsyToHr: explicit HR recipient when there are multiple HRs in the company.
        public string? HrRecipientUserId { get; set; }
        [ForeignKey("HrRecipientUserId")]
        public virtual ApplicationUser? HrRecipient { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        [StringLength(2000)] public string? Notes { get; set; }

        [Required, StringLength(32)]
        public string Status { get; set; } = "Draft";        // Draft / Sent

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EmailSentAt { get; set; }

        [StringLength(200)] public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
    }

    // ─── Psy slice 10: 1-to-1 notification preferences for psychologists ─────
    public class PsyNotificationPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PsychologistId { get; set; }
        [ForeignKey("PsychologistId")]
        public virtual Psychologist? Psychologist { get; set; }

        public bool RemindNewReports { get; set; } = true;
        public bool RemindFollowUp { get; set; } = true;
        public bool AllowHrPatientNotif { get; set; } = false;

        [StringLength(16)]
        public string Frequency { get; set; } = "Daily";    // Daily / Weekly / Monthly
    }

    // ─── HR slice 7: notification preferences (1-to-1 with HrStaff) ─────
    public class HrNotificationPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HrId { get; set; }
        [ForeignKey("HrId")]
        public virtual HrStaff? Hr { get; set; }

        public bool RemindEmployeeCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowEmployeePsyNotif { get; set; } = false;

        [StringLength(16)]
        public string Frequency { get; set; } = "Daily";    // Daily / Weekly / Monthly
    }

    public class MoodTracker
    {
        [Key]
        public int MoodId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        // One of: Overjoyed, Happy, Calm, Neutral, Disappointed, Angry
        [Required]
        [MaxLength(16)]
        public string Feeling { get; set; } = string.Empty;

        // CSV of triggers, e.g. "Work,Family,Hobby"
        // Valid values: Self, Family, School, Work, Friends, Partner, Hobby, Activity, SocialMedia, Entertainment
        [MaxLength(256)]
        public string Triggers { get; set; } = string.Empty;

        // Optional free-text reflection (mood-tracker step 2.5)
        [MaxLength(500)]
        public string? Note { get; set; }

        // The day this mood is for. Enforced unique per (PatientId, MoodDate) via OnModelCreating.
        public DateTime MoodDate { get; set; }

        // When the record was last created/updated.
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }

    public class Journal
    {
        [Key]
        public int JournalId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        // The day this journal entry is for. Unique per (PatientId, JournalDate).
        public DateTime JournalDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // 6-question structured daily check-in (separate from free-write Journal)
    public class JournalCheckIn
    {
        [Key]
        public int CheckInId { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Range(1, 5)] public int FocusScore { get; set; }        // Q1
        [Range(1, 5)] public int AnxietyScore { get; set; }      // Q2  (5 = least anxious)
        [Range(1, 5)] public int SleepScore { get; set; }        // Q3
        [Range(1, 5)] public int MindLoadScore { get; set; }     // Q4  (5 = least burdened)
        [Range(1, 5)] public int EmotionScore { get; set; }      // Q5
        [Range(1, 5)] public int OverallScore { get; set; }      // Q6

        public DateTime CheckInDate { get; set; }                // Unique per (PatientId, CheckInDate)
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }

    // Per-patient notification preferences (1-to-1 with Patient)
    public class PatientNotificationPreference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        public bool RemindMoodCheck { get; set; } = true;
        public bool RemindCounselingSession { get; set; } = true;
        public bool AllowHrPsychologistNotif { get; set; } = true;

        public TimeSpan ReminderTime { get; set; } = new TimeSpan(9, 0, 0);  // default 09:00
    }
}