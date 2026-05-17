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

            // Cek apakah tabel Companies sudah ada datanya. Jika sudah, hentikan proses (jangan duplikat data)
            if (context.Companies.Any())
            {
                return;
            }

            // =====================================
            // 1. BUAT DATA PERUSAHAAN
            // =====================================
            var compA = new Company { Name = "Perusahaan A", Address = "Jakarta Selatan", ContactNumber = "021-1111" };
            var compB = new Company { Name = "Perusahaan B", Address = "Tangerang", ContactNumber = "021-2222" };
            context.Companies.AddRange(compA, compB);
            await context.SaveChangesAsync();

            // =====================================
            // 2. BUAT AKUN PSIKOLOG (Dr. Dina)
            // =====================================
            var userDina = new ApplicationUser
            {
                UserName = "dr.dina@lightenup.com",
                Email = "dr.dina@lightenup.com",
                FullName = "Dr. Dina",
                RoleType = "Psychologist",
                IsApprovedByAdmin = true
            };

            // Password untuk semua user dummy ini adalah "Password123!"
            await userManager.CreateAsync(userDina, "Password123!");

            var psychDina = new Psychologist
            {
                UserId = userDina.Id,
                Specialization = "Psikolog Klinis",
                LicenseNumber = "PSY-2024-8891",
                ExperienceYears = 8,
                PracticeLocation = "Klinik LightenUp"
            };
            context.Psychologists.Add(psychDina);
            await context.SaveChangesAsync();

            // =====================================
            // 3. BUAT AKUN PASIEN (Karyawan)
            // =====================================
            // Pasien 1 (Kaffah)
            var userKaffah = new ApplicationUser { UserName = "kaffah@perusahaana.com", Email = "kaffah@perusahaana.com", FullName = "Kaffah An Nas", RoleType = "Patient", IsApprovedByAdmin = true };
            await userManager.CreateAsync(userKaffah, "Password123!");

            var patientKaffah = new Patient { UserId = userKaffah.Id, CompanyId = compA.CompanyId, Gender = "Laki-laki", MentalHealthStatus = "Sehat" };
            context.Patients.Add(patientKaffah);

            // Pasien 2 (Siti)
            var userSiti = new ApplicationUser { UserName = "siti@perusahaana.com", Email = "siti@perusahaana.com", FullName = "Siti Aisyah", RoleType = "Patient", IsApprovedByAdmin = true };
            await userManager.CreateAsync(userSiti, "Password123!");

            var patientSiti = new Patient { UserId = userSiti.Id, CompanyId = compA.CompanyId, Gender = "Perempuan", MentalHealthStatus = "Beresiko" };
            context.Patients.Add(patientSiti);

            await context.SaveChangesAsync();

            // =====================================
            // 4. BUAT RELASI (ASSIGNMENT PASIEN KE DR. DINA)
            // =====================================
            context.Assignments.AddRange(
                new PatientPsychologistAssignment { PatientId = patientKaffah.PatientId, PsychologistId = psychDina.PsychologistId, Status = "Active" },
                new PatientPsychologistAssignment { PatientId = patientSiti.PatientId, PsychologistId = psychDina.PsychologistId, Status = "Active" }
            );
            await context.SaveChangesAsync();

            // =====================================
            // 5. BUAT CONTOH JADWAL & WORKSHEET UNTUK KAFFAH
            // =====================================
            context.Schedules.Add(new Schedule
            {
                PatientId = patientKaffah.PatientId,
                PsychologistId = psychDina.PsychologistId,
                SessionStart = DateTime.Today.AddDays(1).AddHours(9).AddMinutes(30), // tomorrow 09:30
                DurationMinutes = 60,
                Status = "Scheduled"
            });

            context.Worksheets.Add(new Worksheet
            {
                PatientId = patientKaffah.PatientId,
                PsychologistId = psychDina.PsychologistId,
                TaskName = "Meditasi 30 Menit",
                Deadline = DateTime.Today.AddDays(2),
                Status = "Assigned" // Valid values: Assigned, InProgress, Completed
            });

            await context.SaveChangesAsync();
        }
    }
}