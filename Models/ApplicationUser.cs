using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Models
{
    // Class ini menggantikan tabel 'users' di SQL Anda.
    // Di SSMS, ASP.NET akan menamainya tabel 'AspNetUsers'
    public class ApplicationUser : IdentityUser
    {
        // Kolom tambahan yang Anda butuhkan (dari SQL script)
        public string FullName { get; set; } = string.Empty;

        // Menyimpan nilai enum ('patient', 'psychologist', 'hr')
        public string RoleType { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;

        // Penanda khusus: Psychologist butuh approval HR
        public bool IsApprovedByHR { get; set; } = false;

        // --- RELASI (FOREIGN KEYS) ---
        // Menghubungkan user ini ke profil spesifik mereka
        public virtual Patient? Patient { get; set; }
        public virtual Psychologist? Psychologist { get; set; }
        public virtual HrStaff? HrStaff { get; set; }
    }
}