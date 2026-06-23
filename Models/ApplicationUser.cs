using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Models
{
    // #Class ApplicationUser#
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

        // True = LightenUp Admin has approved the account.
        // - Patient: auto-true on creation (B2C/B2B both self-serve)
        // - Psychologist: starts false; Admin reviews license docs to approve
        // - HR: starts false; Admin reviews company info to approve
        // - Admin: always true (only Admin-created)
        public bool IsApprovedByAdmin { get; set; } = false;

        public virtual Patient? Patient { get; set; }
        public virtual Psychologist? Psychologist { get; set; }
        public virtual HrStaff? HrStaff { get; set; }
    }
}
