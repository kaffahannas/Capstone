using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // DAFTAR TABEL DATABASE (DbSet)
        // ==========================================

        // Tabel Pengguna Dasar
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Psychologist> Psychologists { get; set; }
        public DbSet<HrStaff> HrStaffs { get; set; }

        // Tabel Bisnis & Entitas Baru
        public DbSet<Company> Companies { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PatientPsychologistAssignment> Assignments { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Worksheet> Worksheets { get; set; }
        public DbSet<MoodTracker> MoodTrackers { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<JournalCheckIn> JournalCheckIns { get; set; }
        public DbSet<PatientNotificationPreference> PatientNotificationPreferences { get; set; }

        // HR tables
        public DbSet<PendingEmployee> PendingEmployees { get; set; }
        public DbSet<PsychologistRequest> PsychologistRequests { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<HrNotificationPreference> HrNotificationPreferences { get; set; }

        // Psychologist tables
        public DbSet<PsyNotificationPreference> PsyNotificationPreferences { get; set; }

        // ==========================================
        // KONFIGURASI RELASI (Fluent API)
        // ==========================================
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
            builder.Entity<Company>()
                .HasIndex(c => c.ReferralCode)
                .IsUnique()
                .HasFilter("[ReferralCode] IS NOT NULL");

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

            // 9. One notification-preference row per patient (1-to-1)
            builder.Entity<PatientNotificationPreference>()
                .HasIndex(n => n.PatientId)
                .IsUnique();

            builder.Entity<PatientNotificationPreference>()
                .HasOne(n => n.Patient)
                .WithOne(p => p.NotificationPreference)
                .HasForeignKey<PatientNotificationPreference>(n => n.PatientId)
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
                .OnDelete(DeleteBehavior.Restrict);

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

            // 13. One notification-preference row per HR (1-to-1)
            builder.Entity<HrNotificationPreference>()
                .HasIndex(n => n.HrId)
                .IsUnique();

            builder.Entity<HrNotificationPreference>()
                .HasOne(n => n.Hr)
                .WithOne(h => h.NotificationPreference)
                .HasForeignKey<HrNotificationPreference>(n => n.HrId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // 15. One notification-preference row per psychologist (1-to-1)
            builder.Entity<PsyNotificationPreference>()
                .HasIndex(n => n.PsychologistId)
                .IsUnique();

            builder.Entity<PsyNotificationPreference>()
                .HasOne(n => n.Psychologist)
                .WithOne(p => p.NotificationPreference)
                .HasForeignKey<PsyNotificationPreference>(n => n.PsychologistId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}