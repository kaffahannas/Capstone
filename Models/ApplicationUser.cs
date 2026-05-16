using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // NOTE: This duplicates the AspNetUserRoles table (Identity roles).
        // Treat AspNetUserRoles as the source of truth for authorization
        // ([Authorize(Roles="...")], IsInRoleAsync, etc.). Keep RoleType in sync
        // — or remove it and add a [NotMapped] computed property instead.
        public string RoleType { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApprovedByHR { get; set; } = false;

        public virtual Patient? Patient { get; set; }
        public virtual Psychologist? Psychologist { get; set; }
        public virtual HrStaff? HrStaff { get; set; }
    }
}