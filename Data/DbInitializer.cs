using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LightenUp.Web.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            // Apply any pending EF Core migrations.
            // (Use Migrate(), not EnsureCreated() — EnsureCreated bypasses
            // __EFMigrationsHistory and breaks future `dotnet ef database update`.)
            await context.Database.MigrateAsync();

            // =====================================
            // 1. BUAT DATA PERUSAHAAN
            // =====================================
            var compA = await context.Companies.FirstOrDefaultAsync(c => c.Name == "Perusahaan A");
            if (compA == null)
            {
                compA = new Company { Name = "Perusahaan A", Address = "Jakarta Selatan", ContactNumber = "021-1111" };
                var compB = new Company { Name = "Perusahaan B", Address = "Tangerang", ContactNumber = "021-2222" };
                context.Companies.AddRange(compA, compB);
                await context.SaveChangesAsync();
            }

            // =====================================
            // 1b. BUAT DIVISI PERUSAHAAN (REFERRAL CODES)
            // =====================================
            if (!await context.CompanyDivisions.AnyAsync(d => d.CompanyId == compA.CompanyId))
            {
                context.CompanyDivisions.AddRange(
                    new CompanyDivision { CompanyId = compA.CompanyId, Name = "Pusat", ReferralCode = "A-PUSAT-01" },
                    new CompanyDivision { CompanyId = compA.CompanyId, Name = "IT & Engineering", ReferralCode = "A-IT-123" },
                    new CompanyDivision { CompanyId = compA.CompanyId, Name = "Pemasaran", ReferralCode = "A-MKT-456" }
                );
                await context.SaveChangesAsync();
            }

            // =====================================
            // 1c. BUAT LANGGANAN AKTIF
            // =====================================
            if (!await context.CompanySubscriptions.AnyAsync(s => s.CompanyId == compA.CompanyId && s.Status == "Active"))
            {
                context.CompanySubscriptions.Add(new CompanySubscription
                {
                    CompanyId = compA.CompanyId,
                    PlanName = "Enterprise Plan",
                    StartDate = DateTime.Today.AddDays(-10),
                    EndDate = DateTime.Today.AddYears(1),
                    Status = "Active"
                });
                await context.SaveChangesAsync();
            }

            // =====================================
            // 2. BUAT AKUN PSIKOLOG (Dr. Dina)
            // =====================================
            var userDina = await userManager.FindByEmailAsync("dr.dina@lightenup.com");
            Psychologist psychDina = null;
            if (userDina == null)
            {
                userDina = new ApplicationUser
                {
                    UserName = "dr.dina@lightenup.com",
                    Email = "dr.dina@lightenup.com",
                    EmailConfirmed = true,
                    FullName = "Dr. Dina",
                    RoleType = "Psychologist",
                    IsApprovedByAdmin = true
                };
                await userManager.CreateAsync(userDina, "Password123!");
                await userManager.AddToRoleAsync(userDina, "Psychologist");

                psychDina = new Psychologist
                {
                    UserId = userDina.Id,
                    Specialization = "Psikolog Klinis",
                    LicenseNumber = "PSY-2024-8891",
                    ExperienceYears = 8,
                    PracticeLocation = "Klinik LightenUp",
                    AcceptsB2B = true
                };
                context.Psychologists.Add(psychDina);
                await context.SaveChangesAsync();
            }
            else
            {
                psychDina = await context.Psychologists.FirstOrDefaultAsync(p => p.UserId == userDina.Id);
            }

            // Hubungkan Dr. Dina sebagai partner Perusahaan A jika belum
            await context.Entry(compA).Collection(c => c.PartneredPsychologists).LoadAsync();
            if (!compA.PartneredPsychologists.Any(p => p.PsychologistId == psychDina.PsychologistId))
            {
                compA.PartneredPsychologists.Add(psychDina);
                await context.SaveChangesAsync();
            }

            // =====================================
            // 3. BUAT AKUN HR STAFF (HR Perusahaan A)
            // =====================================
            var userHr = await userManager.FindByEmailAsync("hr@perusahaana.com");
            if (userHr == null)
            {
                userHr = new ApplicationUser
                {
                    UserName = "hr@perusahaana.com",
                    Email = "hr@perusahaana.com",
                    EmailConfirmed = true,
                    FullName = "HR Manager A",
                    RoleType = "HR",
                    IsApprovedByAdmin = true
                };
                await userManager.CreateAsync(userHr, "Password123!");
                await userManager.AddToRoleAsync(userHr, "HR");

                var hrStaff = new HrStaff
                {
                    UserId = userHr.Id,
                    CompanyId = compA.CompanyId,
                    Department = "Human Resources",
                    OnboardingCompletedAt = DateTime.UtcNow
                };
                context.HrStaffs.Add(hrStaff);
                await context.SaveChangesAsync();
            }

            // =====================================
            // 4. BUAT AKUN PASIEN (Karyawan)
            // =====================================
            var userKaffah = await userManager.FindByEmailAsync("kaffah@perusahaana.com");
            Patient patientKaffah = null;
            if (userKaffah == null)
            {
                userKaffah = new ApplicationUser { UserName = "kaffah@perusahaana.com", Email = "kaffah@perusahaana.com", EmailConfirmed = true, FullName = "Kaffah An Nas", RoleType = "Patient", IsApprovedByAdmin = true };
                await userManager.CreateAsync(userKaffah, "Password123!");
                await userManager.AddToRoleAsync(userKaffah, "Patient");

                patientKaffah = new Patient { UserId = userKaffah.Id, CompanyId = compA.CompanyId, Department = "IT & Engineering", Gender = "Laki-laki", MentalHealthStatus = "Sehat" };
                context.Patients.Add(patientKaffah);

                var userSiti = new ApplicationUser { UserName = "siti@perusahaana.com", Email = "siti@perusahaana.com", EmailConfirmed = true, FullName = "Siti Aisyah", RoleType = "Patient", IsApprovedByAdmin = true };
                await userManager.CreateAsync(userSiti, "Password123!");
                await userManager.AddToRoleAsync(userSiti, "Patient");

                var patientSiti = new Patient { UserId = userSiti.Id, CompanyId = compA.CompanyId, Department = "Pemasaran", Gender = "Perempuan", MentalHealthStatus = "Beresiko" };
                context.Patients.Add(patientSiti);

                await context.SaveChangesAsync();

                // =====================================
                // 5. BUAT RELASI (ASSIGNMENT PASIEN KE DR. DINA)
                // =====================================
                context.Assignments.AddRange(
                    new PatientPsychologistAssignment { PatientId = patientKaffah.PatientId, PsychologistId = psychDina.PsychologistId, Status = "Active" },
                    new PatientPsychologistAssignment { PatientId = patientSiti.PatientId, PsychologistId = psychDina.PsychologistId, Status = "Active" }
                );
                await context.SaveChangesAsync();

                // =====================================
                // 6. BUAT CONTOH JADWAL & WORKSHEET UNTUK KAFFAH
                // =====================================
                context.Schedules.Add(new Schedule
                {
                    PatientId = patientKaffah.PatientId,
                    PsychologistId = psychDina.PsychologistId,
                    SessionStart = DateTime.Today.AddDays(1).AddHours(9).AddMinutes(30),
                    DurationMinutes = 60,
                    Status = "Scheduled"
                });

                context.Worksheets.Add(new Worksheet
                {
                    PatientId = patientKaffah.PatientId,
                    PsychologistId = psychDina.PsychologistId,
                    TaskName = "Meditasi 30 Menit",
                    Deadline = DateTime.Today.AddDays(2),
                    Status = "Assigned"
                });

                await context.SaveChangesAsync();
            }

            // =====================================
            // 7. BUAT DUMMY DATA BESAR UNTUK TESTING (V2)
            // =====================================
            if (await context.Patients.CountAsync() < 10)
            {
                // Generate 8 more dummy patients
                var depts = new[] { "Pusat", "IT & Engineering", "Pemasaran", "Keuangan", "Operasional" };
                var statuses = new[] { "Sehat", "Sehat", "Sehat", "Beresiko", "Beresiko", "Bahaya" };
                var genders = new[] { "Laki-laki", "Perempuan" };
                
                var newPatients = new System.Collections.Generic.List<Patient>();
                for (int i = 1; i <= 8; i++)
                {
                    var dummyUser = new ApplicationUser 
                    { 
                        UserName = $"dummy{i}@perusahaana.com", 
                        Email = $"dummy{i}@perusahaana.com", 
                        EmailConfirmed = true, 
                        FullName = $"Dummy Karyawan {i}", 
                        RoleType = "Patient", 
                        IsApprovedByAdmin = true 
                    };
                    await userManager.CreateAsync(dummyUser, "Password123!");
                    await userManager.AddToRoleAsync(dummyUser, "Patient");

                    var p = new Patient 
                    { 
                        UserId = dummyUser.Id, 
                        CompanyId = compA.CompanyId, 
                        Department = depts[i % depts.Length], 
                        Gender = genders[i % 2], 
                        MentalHealthStatus = statuses[i % statuses.Length],
                        OnboardingCompletedAt = DateTime.UtcNow.AddDays(-i)
                    };
                    context.Patients.Add(p);
                    newPatients.Add(p);
                }
                await context.SaveChangesAsync();

                // Generate 4 more dummy psychologists
                var specs = new[] { "Psikolog Klinis", "Psikolog Pendidikan", "Psikolog Industri", "Psikolog Anak" };
                var newPsychs = new System.Collections.Generic.List<Psychologist>();
                for (int i = 1; i <= 4; i++)
                {
                    var pUser = new ApplicationUser 
                    { 
                        UserName = $"psikolog{i}@lightenup.com", 
                        Email = $"psikolog{i}@lightenup.com", 
                        EmailConfirmed = true, 
                        FullName = $"Dr. Spesialis {i}", 
                        RoleType = "Psychologist", 
                        IsApprovedByAdmin = true 
                    };
                    await userManager.CreateAsync(pUser, "Password123!");
                    await userManager.AddToRoleAsync(pUser, "Psychologist");

                    var psych = new Psychologist
                    {
                        UserId = pUser.Id,
                        Specialization = specs[i % specs.Length],
                        LicenseNumber = $"PSY-900{i}",
                        ExperienceYears = 3 + i,
                        PracticeLocation = "Klinik LightenUp Cabang " + i,
                        AcceptsB2B = true
                    };
                    context.Psychologists.Add(psych);
                    newPsychs.Add(psych);
                    compA.PartneredPsychologists.Add(psych);
                }
                await context.SaveChangesAsync();

                // Generate lots of schedules, worksheets, reports
                var random = new Random();
                var allPatients = await context.Patients.ToListAsync();
                var allPsychs = await context.Psychologists.ToListAsync();

                foreach (var pat in allPatients)
                {
                    // Assign to a random psychologist
                    var psy = allPsychs[random.Next(allPsychs.Count)];
                    if (!await context.Assignments.AnyAsync(a => a.PatientId == pat.PatientId && a.PsychologistId == psy.PsychologistId))
                    {
                        context.Assignments.Add(new PatientPsychologistAssignment { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, Status = "Active" });
                    }

                    // Schedules (past, today, future)
                    context.Schedules.Add(new Schedule { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, SessionStart = DateTime.Today.AddDays(-random.Next(1, 10)).AddHours(10), DurationMinutes = 60, Status = "Completed" });
                    context.Schedules.Add(new Schedule { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, SessionStart = DateTime.Today.AddHours(random.Next(8, 17)), DurationMinutes = 60, Status = random.Next(10) > 5 ? "Scheduled" : "Completed" });
                    context.Schedules.Add(new Schedule { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, SessionStart = DateTime.Today.AddDays(random.Next(1, 10)).AddHours(14), DurationMinutes = 60, Status = "Scheduled" });

                    // Worksheets
                    context.Worksheets.Add(new Worksheet { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, TaskName = "Jurnal Harian", Deadline = DateTime.Today.AddDays(-2), Status = "Completed" });
                    context.Worksheets.Add(new Worksheet { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, TaskName = "Refleksi Emosi", Deadline = DateTime.Today.AddDays(3), Status = "Assigned" });

                    // Reports
                    if (random.Next(10) > 4)
                    {
                        context.Reports.Add(new Report { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, EmailSubject = $"Laporan Evaluasi {pat.PatientId}", Notes = "Kondisi stabil, perlu pemantauan lebih lanjut.", Status = "Sent", CreatedAt = DateTime.UtcNow.AddDays(-2), Direction = "HrToPsy" });
                    }
                    else
                    {
                        context.Reports.Add(new Report { PatientId = pat.PatientId, PsychologistId = psy.PsychologistId, EmailSubject = $"Draft Laporan {pat.PatientId}", Notes = "Masih dalam tahap analisa...", Status = "Draft", CreatedAt = DateTime.UtcNow, Direction = "HrToPsy" });
                    }
                }
                
                await context.SaveChangesAsync();
            }
        }
    }
}