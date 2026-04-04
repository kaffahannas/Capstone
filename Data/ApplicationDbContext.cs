using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
﻿using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    // PERHATIKAN: Kita ubah dari IdentityDbContext biasa
    // menjadi IdentityDbContext<ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Daftarkan tabel-tabel spesifik di sini agar dibuatkan di SSMS
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Psychologist> Psychologists { get; set; }
        public DbSet<HrStaff> HrStaffs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Opsional: Jika Anda ingin tabel utamanya tetap bernama 'users'
            // alih-alih 'AspNetUsers', hapus tanda komentar di bawah ini:
            // builder.Entity<ApplicationUser>().ToTable("users");
        }
    }
}
}