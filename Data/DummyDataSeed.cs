using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Data;

/// <summary>
/// Compact demo dataset — one row (or a few) per feature/table.
/// Idempotent: skips if <see cref="CompanyName"/> already exists.
/// Reset DB: drop database lalu jalankan dotnet ef database update
/// </summary>
public static class DummyDataSeed
{
    public const string CompanyName = "PT Sasindo";
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        await context.Database.MigrateAsync();

        if (await context.Companies.AnyAsync(c => c.Name == CompanyName))
            return;

        var today = DateTime.Today;
        var utcNow = DateTime.UtcNow;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // ── Company & divisions ─────────────────────────────────────────────
        var company = new Company
        {
            Name = CompanyName,
            Address = "Jl. Sudirman No. 45, Jakarta",
            ContactNumber = "021-5550100",
            ContactEmail = "hrd@sasindo.com",
            RegistrationNumber = "REG-SAS-2024",
            CreatedAt = utcNow.AddMonths(-6)
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var divPusat = new CompanyDivision { CompanyId = company.CompanyId, Name = "Pusat", ReferralCode = "SAS-PUSAT" };
        var divIt = new CompanyDivision { CompanyId = company.CompanyId, Name = "IT & Engineering", ReferralCode = "SAS-IT-01" };
        var divHr = new CompanyDivision { CompanyId = company.CompanyId, Name = "Human Resources", ReferralCode = "SAS-HR-01" };
        context.CompanyDivisions.AddRange(divPusat, divIt, divHr);
        await context.SaveChangesAsync();

        var companySub = new CompanySubscription
        {
            CompanyId = company.CompanyId,
            PlanName = "Perusahaan 25 Karyawan",
            EmployeeLimit = 25,
            MaxSessionsPerMonth = 4,
            StartDate = today.AddDays(-30),
            EndDate = today.AddYears(1),
            Status = "Active"
        };
        context.CompanySubscriptions.Add(companySub);
        await context.SaveChangesAsync();

        context.PaymentTransactions.Add(new PaymentTransaction
        {
            CompanyId = company.CompanyId,
            CompanySubscriptionId = companySub.CompanySubscriptionId,
            MerchantOrderId = $"SEED-B2B-{companySub.CompanySubscriptionId}",
            Amount = 10_000_000m,
            PlanName = companySub.PlanName,
            PaymentStatus = "paid",
            PaidAt = utcNow.AddDays(-29),
            CreatedAt = utcNow.AddDays(-30)
        });

        context.PendingEmployees.Add(new PendingEmployee
        {
            CompanyId = company.CompanyId,
            DivisionId = divIt.DivisionId,
            FullName = "Calon Karyawan Baru",
            Email = "calon@sasindo.com",
            EmployeeId = "EMP-NEW-001",
            CreatedAt = utcNow.AddDays(-3)
        });

        // ── Psychologists ───────────────────────────────────────────────────
        var psychDinaUser = await CreateUserAsync(userManager, "dr.dina@lightenup.com", "Dr. Dina Wijaya", "Psychologist");
        var psychAndiUser = await CreateUserAsync(userManager, "dr.andi@lightenup.com", "Dr. Andi Pratama", "Psychologist");
        var psychPendingUser = await CreateUserAsync(userManager, "dr.baru@lightenup.com", "Dr. Baru Mendaftar", "Psychologist", approved: false);

        var psychDina = new Psychologist
        {
            UserId = psychDinaUser.Id,
            Specialization = "Psikolog Klinis",
            LicenseNumber = "PSY-2024-001",
            SiapNumber = "SIAP-001",
            ExperienceYears = 8,
            PracticeLocation = "Jakarta Selatan",
            LastDegree = "M.Psi.",
            University = "Universitas Indonesia",
            AcademicDocumentUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf",
            StrDocumentUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf",
            AcceptsB2B = true,
            IsAvailable = true,
            OnboardingCompletedAt = utcNow.AddMonths(-2),
            Bio = "Spesialis kecemasan kerja dan burnout."
        };
        var psychAndi = new Psychologist
        {
            UserId = psychAndiUser.Id,
            Specialization = "Psikolog Industri & Organisasi",
            LicenseNumber = "PSY-2024-002",
            SiapNumber = "SIAP-002",
            ExperienceYears = 5,
            PracticeLocation = "Jakarta Pusat",
            LastDegree = "M.Psi.",
            University = "Universitas Gadjah Mada",
            AcceptsB2B = true,
            IsAvailable = true,
            OnboardingCompletedAt = utcNow.AddMonths(-1)
        };
        var psychPending = new Psychologist
        {
            UserId = psychPendingUser.Id,
            Specialization = "Psikolog Klinis",
            LicenseNumber = "PSY-PENDING-01",
            SiapNumber = "SIAP-PENDING",
            ExperienceYears = 2,
            PracticeLocation = "Bandung",
            OnboardingCompletedAt = utcNow.AddDays(-2)
        };
        context.Psychologists.AddRange(psychDina, psychAndi, psychPending);
        await context.SaveChangesAsync();

        company.PartneredPsychologists.Add(psychDina);
        company.PartneredPsychologists.Add(psychAndi);
        await context.SaveChangesAsync();

        context.PayrollSettings.AddRange(
            new PsychologistPayrollSetting { PsychologistId = psychDina.PsychologistId, PsychologistPercentage = 40, Status = "Active" },
            new PsychologistPayrollSetting { PsychologistId = psychAndi.PsychologistId, PsychologistPercentage = 45, Status = "Active" });

        var lastMonth = monthStart.AddMonths(-1);
        context.MonthlyPayouts.Add(new MonthlyPayout
        {
            PsychologistId = psychDina.PsychologistId,
            Month = lastMonth.Month,
            Year = lastMonth.Year,
            TotalAmount = 180_000m,
            Status = "Paid",
            PaidAt = monthStart.AddDays(-2),
            ProofOfTransferFilePath = "/uploads/demo/payout-proof.pdf"
        });

        // ── HR ──────────────────────────────────────────────────────────────
        var hrUser = await CreateUserAsync(userManager, "hr@sasindo.com", "Rina HR Sasindo", "HR");
        var hrStaff = new HrStaff
        {
            UserId = hrUser.Id,
            CompanyId = company.CompanyId,
            Department = "Human Resources",
            EmployeeId = "HR-001",
            OnboardingCompletedAt = utcNow.AddMonths(-3)
        };
        context.HrStaffs.Add(hrStaff);
        await context.SaveChangesAsync();

        // ── Patients (3 B2B + 2 B2C) ────────────────────────────────────────
        var patientKaffah = await CreatePatientAsync(userManager, context, "kaffah@sasindo.com", "Kaffah An Nas",
            company.CompanyId, divIt.DivisionId, "EMP-001", "Laki-laki", "Sehat");
        var patientSiti = await CreatePatientAsync(userManager, context, "siti@sasindo.com", "Siti Aisyah",
            company.CompanyId, divHr.DivisionId, "EMP-002", "Perempuan", "Beresiko");
        var patientBudi = await CreatePatientAsync(userManager, context, "budi@sasindo.com", "Budi Santoso",
            company.CompanyId, divPusat.DivisionId, "EMP-003", "Laki-laki", "Bahaya");
        var patientRiza = await CreatePatientAsync(userManager, context, "riza@gmail.com", "Riza Putri",
            null, null, null, "Perempuan", "Sehat");
        var patientMaya = await CreatePatientAsync(userManager, context, "maya@gmail.com", "Maya Lestari",
            null, null, null, "Perempuan", "Beresiko");

        await SeedB2CSubscriptionAsync(context, patientRiza, 99_000m, "Basic Bulanan");
        await SeedB2CSubscriptionAsync(context, patientMaya, 99_000m, "Basic Bulanan");

        // ── Assignments ─────────────────────────────────────────────────────
        context.Assignments.AddRange(
            ActiveAssignment(patientKaffah.PatientId, psychDina.PsychologistId, hrUser.Id, 400_000m),
            new PatientPsychologistAssignment
            {
                PatientId = patientSiti.PatientId,
                PsychologistId = psychDina.PsychologistId,
                AssignedByHrUserId = hrUser.Id,
                Status = "PendingCancellationByAdmin",
                AssignedAt = utcNow.AddDays(-45),
                SlotValue = 400_000m,
                MaxSessionsPerMonth = 4,
                PsychologistRevenuePercentage = 40,
                CancellationRequestedByUserId = hrUser.Id,
                CancellationReason = "Karyawan pindah divisi, perlu psikolog lain.",
                CancellationRequestedAt = utcNow.AddDays(-2)
            },
            ActiveAssignment(patientRiza.PatientId, psychDina.PsychologistId, null, 99_000m),
            new PatientPsychologistAssignment
            {
                PatientId = patientBudi.PatientId,
                PsychologistId = psychAndi.PsychologistId,
                Status = "PendingPsychologistApproval",
                RequestedByUserId = patientBudi.UserId,
                RequestedByRole = "Patient",
                AssignedAt = utcNow.AddDays(-1)
            },
            new PatientPsychologistAssignment
            {
                PatientId = patientMaya.PatientId,
                PsychologistId = psychAndi.PsychologistId,
                Status = "PendingAdminApproval",
                RequestedByUserId = psychAndi.UserId,
                RequestedByRole = "Psychologist",
                AssignedAt = utcNow.AddDays(-1)
            });

        // Siti still has completed sessions/worksheets while cancellation is pending
        context.Schedules.Add(
            new Schedule
            {
                PatientId = patientSiti.PatientId,
                PsychologistId = psychDina.PsychologistId,
                SessionStart = monthStart.AddDays(5).AddHours(14),
                DurationMinutes = 60,
                Status = "Completed",
                SlotValue = 400_000m,
                AppliedPercentage = 40
            });

        context.PatientAdminAssignmentRequests.Add(new PatientAdminAssignmentRequest
        {
            PatientId = patientMaya.PatientId,
            PreferredPsychologistId = psychDina.PsychologistId,
            Reason = "Butuh psikolog perempuan untuk konseling awal.",
            Status = "Pending",
            CreatedAt = utcNow.AddDays(-1)
        });

        context.HrEmployeeRemovalRequests.Add(new HrEmployeeRemovalRequest
        {
            PatientId = patientBudi.PatientId,
            RequestedByHrUserId = hrUser.Id,
            Reason = "Karyawan resign — mohon nonaktifkan akses counseling.",
            Status = "Pending",
            CreatedAt = utcNow.AddDays(-1)
        });

        context.CompanyPsychologistRequests.Add(new CompanyPsychologistRequest
        {
            CompanyId = company.CompanyId,
            PsychologistId = psychAndi.PsychologistId,
            Notes = "HR meminta Dr. Andi sebagai partner tambahan untuk divisi IT.",
            Status = "Pending",
            RequestDate = utcNow.AddDays(-2)
        });

        // ── Schedules ───────────────────────────────────────────────────────
        context.Schedules.AddRange(
            new Schedule
            {
                PatientId = patientKaffah.PatientId,
                PsychologistId = psychDina.PsychologistId,
                SessionStart = monthStart.AddDays(2).AddHours(10),
                DurationMinutes = 60,
                Status = "Completed",
                MeetingLink = "https://meet.google.com/demo-kaffah",
                Notes = "Sesi follow-up mingguan",
                SlotValue = 400_000m,
                AppliedPercentage = 40
            },
            new Schedule
            {
                PatientId = patientRiza.PatientId,
                PsychologistId = psychDina.PsychologistId,
                SessionStart = today.AddDays(2).AddHours(9),
                DurationMinutes = 60,
                Status = "Scheduled",
                MeetingLink = "https://meet.google.com/demo-riza"
            },
            new Schedule
            {
                PatientId = patientKaffah.PatientId,
                PsychologistId = psychDina.PsychologistId,
                SessionStart = today.AddDays(-3).AddHours(11),
                DurationMinutes = 60,
                Status = "Cancelled",
                Notes = "Dibatalkan oleh pasien"
            });

        // ── Worksheets ──────────────────────────────────────────────────────
        context.Worksheets.AddRange(
            new Worksheet
            {
                PatientId = patientKaffah.PatientId,
                PsychologistId = psychDina.PsychologistId,
                TaskName = "Jurnal Gratitude 5 Menit",
                Description = "Tulis 3 hal yang disyukuri hari ini.",
                Deadline = today.AddDays(3),
                Status = "Assigned",
                CreatedAt = utcNow.AddDays(-1)
            },
            new Worksheet
            {
                PatientId = patientSiti.PatientId,
                PsychologistId = psychDina.PsychologistId,
                TaskName = "Latihan Pernapasan 4-7-8",
                Description = "Lakukan 3 siklus sebelum tidur.",
                Deadline = today.AddDays(-1),
                Status = "Completed",
                Note = "Sudah mencoba, terasa lebih tenang.",
                SubmittedAt = utcNow.AddDays(-1),
                ReviewedAt = utcNow,
                PsychologistFeedback = "Bagus, lanjutkan rutinitas ini.",
                CreatedAt = utcNow.AddDays(-5)
            });

        context.PsychologistRequests.AddRange(
            new PsychologistRequest
            {
                RequestedByHrUserId = hrUser.Id,
                RequesterRole = "HR",
                PatientId = patientSiti.PatientId,
                PsychologistId = psychDina.PsychologistId,
                RequestType = "Worksheet",
                ProposedTaskName = "Refleksi mingguan stressor kerja",
                ProposedDeadline = today.AddDays(7),
                Notes = "Mohon worksheet tambahan untuk Siti.",
                Status = "Pending",
                CreatedAt = utcNow.AddDays(-1)
            },
            new PsychologistRequest
            {
                RequestedByPatientUserId = patientRiza.UserId,
                RequesterRole = "Patient",
                PatientId = patientRiza.PatientId,
                PsychologistId = psychDina.PsychologistId,
                RequestType = "Schedule",
                ProposedSessionDate = today.AddDays(5).AddHours(15),
                Notes = "Ingin sesi tambahan membahas kecemasan.",
                Status = "Pending",
                CreatedAt = utcNow.AddHours(-6)
            });

        // ── Mood, journal, check-in ───────────────────────────────────────────
        foreach (var (patient, feeling, status) in new[]
        {
            (patientKaffah, "Happy", "Sehat"),
            (patientSiti, "Disappointed", "Beresiko"),
            (patientBudi, "Angry", "Bahaya"),
            (patientRiza, "Calm", "Sehat"),
            (patientMaya, "Neutral", "Beresiko")
        })
        {
            context.MoodTrackers.Add(new MoodTracker
            {
                PatientId = patient.PatientId,
                Feeling = feeling,
                Triggers = "Work,Family",
                Note = "Demo mood entry",
                FocusScore = 4,
                AnxietyScore = 3,
                SleepScore = 4,
                MindLoadScore = 3,
                EmotionScore = 4,
                MoodDate = today,
                RecordedAt = utcNow
            });

            context.Journals.Add(new Journal
            {
                PatientId = patient.PatientId,
                Title = "Refleksi hari ini",
                Content = "Ini catatan demo journal untuk pengujian fitur statistik dan profil.",
                JournalDate = today,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });

            context.JournalCheckIns.Add(new JournalCheckIn
            {
                PatientId = patient.PatientId,
                FocusScore = 4,
                AnxietyScore = 3,
                SleepScore = 4,
                MindLoadScore = 3,
                EmotionScore = 4,
                OverallScore = 4,
                CheckInDate = today,
                RecordedAt = utcNow
            });

            patient.MentalHealthStatus = status;
        }

        // ── Reports ─────────────────────────────────────────────────────────
        context.Reports.AddRange(
            new Report
            {
                Direction = "HrToPsy",
                ReportedByHrUserId = hrUser.Id,
                PatientId = patientBudi.PatientId,
                PsychologistId = psychDina.PsychologistId,
                Notes = "Budi menunjukkan penurunan produktivitas dan konflik tim.",
                Status = "Sent",
                EmailSubject = "Laporan HR — Budi Santoso",
                EmailBody = "Mohon evaluasi dan rencana intervensi.",
                EmailSentAt = utcNow.AddDays(-1),
                CreatedAt = utcNow.AddDays(-2)
            },
            new Report
            {
                Direction = "HrToPsy",
                ReportedByHrUserId = hrUser.Id,
                PatientId = patientSiti.PatientId,
                PsychologistId = psychDina.PsychologistId,
                Notes = "Draft laporan bulanan untuk review HR.",
                Status = "Draft",
                EmailSubject = "Draft — Siti Aisyah",
                CreatedAt = utcNow
            });

        await context.SaveChangesAsync();
    }

    private static PatientPsychologistAssignment ActiveAssignment(
        int patientId, int psychologistId, string? hrUserId, decimal slotValue) =>
        new()
        {
            PatientId = patientId,
            PsychologistId = psychologistId,
            AssignedByHrUserId = hrUserId,
            Status = "Active",
            AssignedAt = DateTime.UtcNow.AddDays(-20),
            SlotValue = slotValue,
            MaxSessionsPerMonth = 4,
            PsychologistRevenuePercentage = 40
        };

    private static async Task SeedB2CSubscriptionAsync(
        ApplicationDbContext context, Patient patient, decimal amount, string planName)
    {
        var sub = new Subscription
        {
            PatientId = patient.PatientId,
            PlanName = planName,
            Status = "Active",
            StartDate = DateTime.Today.AddDays(-15),
            EndDate = DateTime.Today.AddMonths(1),
            MaxSessionsPerMonth = 4
        };
        context.Subscriptions.Add(sub);
        await context.SaveChangesAsync();

        context.PaymentTransactions.Add(new PaymentTransaction
        {
            PatientId = patient.PatientId,
            SubscriptionId = sub.SubscriptionId,
            MerchantOrderId = $"SEED-B2C-{patient.PatientId}-{sub.SubscriptionId}",
            Amount = amount,
            PlanName = planName,
            PaymentStatus = "paid",
            PaidAt = DateTime.UtcNow.AddDays(-14),
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        });
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string role,
        bool approved = true)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            RoleType = role,
            IsApprovedByAdmin = approved
        };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Gagal membuat user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task<Patient> CreatePatientAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        string email,
        string fullName,
        int? companyId,
        int? divisionId,
        string? employeeId,
        string gender,
        string mentalHealthStatus)
    {
        var user = await CreateUserAsync(userManager, email, fullName, "Patient");
        var patient = new Patient
        {
            UserId = user.Id,
            CompanyId = companyId,
            DivisionId = divisionId,
            EmployeeId = employeeId,
            Gender = gender,
            MentalHealthStatus = mentalHealthStatus,
            EmploymentStatus = "active",
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-10),
            TermsAcceptedAt = DateTime.UtcNow.AddDays(-10),
            DateOfBirth = new DateTime(1995, 6, 15)
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient;
    }
}
