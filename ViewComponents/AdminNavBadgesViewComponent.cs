using LightenUp.Web.Data;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.ViewComponents;

public class AdminNavBadgesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public AdminNavBadgesViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(string type = "Assignments")
    {
        int count = 0;
        if (type == "Approvals")
        {
            var psyPending = await _context.Assignments.CountAsync(a => a.Status == "PendingAdminApproval");
            var cancelPending = await _context.Assignments.CountAsync(a => a.Status == "PendingCancellationByAdmin");
            var accPending = await _context.Users.CountAsync(u => u.IsApprovedByAdmin == false && u.RoleType != "Patient" && u.RoleType != "HR");
            var removalPending = await _context.HrEmployeeRemovalRequests.CountAsync(r => r.Status == "Pending");
            count = psyPending + cancelPending + accPending + removalPending;
        }
        else if (type == "Assignments")
        {
            var patientPending = await _context.PatientAdminAssignmentRequests.CountAsync(r => r.Status == "Pending");
            var b2bPending = await _context.CompanyPsychologistRequests.CountAsync(r => r.PsychologistId == null && r.Status == "Pending");
            count = patientPending + b2bPending;
        }

        return View(new AdminNavBadgesViewModel
        {
            Count = count
        });
    }
}
