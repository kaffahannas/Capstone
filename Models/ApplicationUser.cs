using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string RoleType { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApprovedByHR { get; set; } = false;

        public virtual Patient? Patient { get; set; }
        public virtual Psychologist? Psychologist { get; set; }
        public virtual HrStaff? HrStaff { get; set; }
    }
}