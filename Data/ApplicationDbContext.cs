using LightenUp.Web.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using DpKey = Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Data
{
    // #Class ApplicationDbContext#
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        // #Bagian DbSet Tabel#
        // ==========================================
        // DAFTAR TABEL DATABASE (DbSet)
        // ==========================================

        // Tabel Pengguna Dasar
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Psychologist> Psychologists { get; set; }
        public DbSet<HrStaff> HrStaffs { get; set; }

        // Tabel Bisnis & Entitas Baru
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyDivision> CompanyDivisions { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PatientPsychologistAssignment> Assignments { get; set; }
        public DbSet<PsychologistSubscription> PsychologistSubscriptions { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Worksheet> Worksheets { get; set; }
        public DbSet<MoodTracker> MoodTrackers { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<JournalCheckIn> JournalCheckIns { get; set; }

        // HR tables
        public DbSet<PendingEmployee> PendingEmployees { get; set; }
        public DbSet<HrEmployeeRemovalRequest> HrEmployeeRemovalRequests { get; set; }
        public DbSet<PsychologistRequest> PsychologistRequests { get; set; }
        public DbSet<CompanyPsychologistRequest> CompanyPsychologistRequests { get; set; }
        public DbSet<Report> Reports { get; set; }

        // Psychologist tables

        public DbSet<PsychologistPayrollSetting> PayrollSettings { get; set; }
        public DbSet<MonthlyPayout> MonthlyPayouts { get; set; }

        // #Bagian Fluent API Relasi#
        // ==========================================
        // KONFIGURASI RELASI (Fluent API)
        // ==========================================

        // #Function OnModelCreating#
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Mencegah error "Multiple Cascade Paths" di SQL Server
            // dengan mematikan penghapusan otomatis (Cascade Delete) pada tabel yang berelasi ganda.

            // 1. Relasi Assignment (Penugasan)
            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.Psychologist)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1b. AssignedByHr -> AspNetUsers (HR staff member who created the assignment).
            // Optional FK; SetNull on delete so historical assignments are preserved.
            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.AssignedByHr)
                .WithMany()
                .HasForeignKey(a => a.AssignedByHrUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // 1c. RequestedBy, CancellationRequestedBy, DecisionBy — Restrict avoids SQL Server multiple-cascade-path errors.
            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.RequestedBy)
                .WithMany()
                .HasForeignKey(a => a.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.CancellationRequestedBy)
                .WithMany()
                .HasForeignKey(a => a.CancellationRequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientPsychologistAssignment>()
                .HasOne(a => a.DecisionBy)
                .WithMany()
                .HasForeignKey(a => a.DecisionByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientPsychologistAssignment>()
                .Property(a => a.SlotValue)
                .HasColumnType("decimal(14,2)");

            builder.Entity<PatientPsychologistAssignment>()
                .Property(a => a.PsychologistRevenuePercentage)
                .HasColumnType("decimal(5,2)");

            // 1d. PsychologistSubscription FKs
            builder.Entity<PsychologistSubscription>()
                .HasOne(s => s.Psychologist)
                .WithMany(p => p.MitraSubscriptions)
                .HasForeignKey(s => s.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.PsychologistSubscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.PsychologistSubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            // 1e. Patient sponsor psychologist FK
            builder.Entity<Patient>()
                .HasOne(p => p.SponsorPsychologist)
                .WithMany()
                .HasForeignKey(p => p.SponsorPsychologistId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // 1f. Subscription -> Psychologist (B2C terikat ke 1 psikolog)
            builder.Entity<Subscription>()
                .HasOne(s => s.Psychologist)
                .WithMany()
                .HasForeignKey(s => s.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // 1g. MitraReferralCode unique (filtered: ignores NULLs)
            builder.Entity<Psychologist>()
                .HasIndex(p => p.MitraReferralCode)
                .IsUnique()
                .HasFilter("[MitraReferralCode] IS NOT NULL");

            builder.Entity<Psychologist>()
                .Property(p => p.PricePerMonth)
                .HasColumnType("decimal(14,2)");

            // 2. Relasi Schedule (Jadwal)
            builder.Entity<Schedule>()
                .HasOne(s => s.Patient)
                .WithMany(p => p.Schedules)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Schedule>()
                .HasOne(s => s.Psychologist)
                .WithMany(p => p.Schedules)
                .HasForeignKey(s => s.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Relasi Worksheet (Tugas)
            builder.Entity<Worksheet>()
                .HasOne(w => w.Patient)
                .WithMany(p => p.Worksheets)
                .HasForeignKey(w => w.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Worksheet>()
                .HasOne(w => w.Psychologist)
                .WithMany(p => p.Worksheets)
                .HasForeignKey(w => w.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Unique referral code per company (filtered: ignores NULLs)
            // Note: Now ReferralCode is primarily on CompanyDivision
            builder.Entity<Company>()
                .HasIndex(c => c.ReferralCode)
                .IsUnique()
                .HasFilter("[ReferralCode] IS NOT NULL");

            builder.Entity<CompanyDivision>()
                .HasIndex(d => d.ReferralCode)
                .IsUnique()
                .HasFilter("[ReferralCode] IS NOT NULL");

            builder.Entity<CompanyDivision>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Divisions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. EmployeeId is unique within a single company.
            //    A single null EmployeeId per (CompanyId, EmployeeId) is allowed; SQL Server
            //    treats multiple NULLs as distinct only with a filtered index.
            builder.Entity<Patient>()
                .HasIndex(p => new { p.CompanyId, p.EmployeeId })
                .IsUnique()
                .HasFilter("[CompanyId] IS NOT NULL AND [EmployeeId] IS NOT NULL");

            // 6. One mood per patient per day (mood tracker is editable)
            builder.Entity<MoodTracker>()
                .HasIndex(m => new { m.PatientId, m.MoodDate })
                .IsUnique();

            builder.Entity<MoodTracker>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.MoodLogs)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // 7. One free-write journal entry per patient per day
            builder.Entity<Journal>()
                .HasIndex(j => new { j.PatientId, j.JournalDate })
                .IsUnique();

            builder.Entity<Journal>()
                .HasOne(j => j.Patient)
                .WithMany(p => p.Journals)
                .HasForeignKey(j => j.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // 8. One structured check-in per patient per day
            builder.Entity<JournalCheckIn>()
                .HasIndex(c => new { c.PatientId, c.CheckInDate })
                .IsUnique();

            builder.Entity<JournalCheckIn>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.JournalCheckIns)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);



            // 10. PendingEmployee: unique (CompanyId, Email)
            builder.Entity<PendingEmployee>()
                .HasIndex(p => new { p.CompanyId, p.Email })
                .IsUnique();

            builder.Entity<PendingEmployee>()
                .HasOne(p => p.Company)
                .WithMany()
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PendingEmployee>()
                .HasOne(p => p.ClaimedByPatient)
                .WithMany()
                .HasForeignKey(p => p.ClaimedByPatientId)
                .OnDelete(DeleteBehavior.SetNull);

            // 11. PsychologistRequest — Restrict patient/psy cascade to avoid multiple paths
            builder.Entity<PsychologistRequest>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PsychologistRequest>()
                .HasOne(r => r.Psychologist)
                .WithMany()
                .HasForeignKey(r => r.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PsychologistRequest>()
                .HasOne(r => r.RequestedByHr)
                .WithMany()
                .HasForeignKey(r => r.RequestedByHrUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<PsychologistRequest>()
                .HasOne(r => r.RequestedByPatient)
                .WithMany()
                .HasForeignKey(r => r.RequestedByPatientUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // 16. PsychologistPayrollSetting — one per psychologist
            builder.Entity<PsychologistPayrollSetting>()
                .HasIndex(p => p.PsychologistId)
                .IsUnique();

            builder.Entity<PsychologistPayrollSetting>()
                .HasOne(p => p.Psychologist)
                .WithMany()
                .HasForeignKey(p => p.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PsychologistPayrollSetting>()
                .Property(p => p.SessionRate)
                .HasColumnType("decimal(14,2)");

            builder.Entity<PsychologistPayrollSetting>()
                .Property(p => p.PsychologistPercentage)
                .HasColumnType("decimal(5,2)");

            builder.Entity<PsychologistPayrollSetting>()
                .HasOne(p => p.UpdatedByAdmin)
                .WithMany()
                .HasForeignKey(p => p.UpdatedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // 12. Report — Restrict cascade
            builder.Entity<Report>()
                .HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.Psychologist)
                .WithMany()
                .HasForeignKey(r => r.PsychologistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.ReportedByHr)
                .WithMany()
                .HasForeignKey(r => r.ReportedByHrUserId)
                .OnDelete(DeleteBehavior.Restrict);



            // 14. Report — the two new optional FKs (Direction = "PsyToHr" path)
            builder.Entity<Report>()
                .HasOne(r => r.ReportedByPsy)
                .WithMany()
                .HasForeignKey(r => r.ReportedByPsyUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.HrRecipient)
                .WithMany()
                .HasForeignKey(r => r.HrRecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ReportedByHrUserId is now nullable — relax the existing FK
            builder.Entity<Report>()
                .HasOne(r => r.ReportedByHr)
                .WithMany()
                .HasForeignKey(r => r.ReportedByHrUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);



            builder.Entity<PaymentTransaction>()
                .HasIndex(p => p.MerchantOrderId)
                .IsUnique();

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.Subscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.Company)
                .WithMany()
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<PaymentTransaction>()
                .HasOne(p => p.CompanySubscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.CompanySubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<CompanySubscription>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}