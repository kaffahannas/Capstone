using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.ViewComponents;

public class PsyNavBadgesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PsyNavBadgesViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole("Psychologist"))
            return View(new PsyNavBadgesViewModel());

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
            return View(new PsyNavBadgesViewModel());

        var psyId = await _context.Psychologists
            .Where(p => p.UserId == user.Id)
            .Select(p => p.PsychologistId)
            .FirstOrDefaultAsync();

        if (psyId == 0)
            return View(new PsyNavBadgesViewModel());

        var requestCount = await _context.PsychologistRequests
            .CountAsync(r => r.PsychologistId == psyId && r.Status == "Pending");

        var assignmentCount = await _context.Assignments
            .CountAsync(a => a.PsychologistId == psyId && a.Status == "PendingPsychologistApproval");

        return View(new PsyNavBadgesViewModel { Count = requestCount + assignmentCount });
    }
}
