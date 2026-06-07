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

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var psyPending = await _context.Assignments.CountAsync(a => a.Status == "PendingAdminApproval");
        var cancelPending = await _context.Assignments.CountAsync(a => a.Status == "PendingCancellationByAdmin");
        var patientReq = await _context.PatientAdminAssignmentRequests.CountAsync(r => r.Status == "Pending");

        return View(new AdminNavBadgesViewModel
        {
            AssignmentBadgeCount = psyPending + cancelPending + patientReq
        });
    }
}
