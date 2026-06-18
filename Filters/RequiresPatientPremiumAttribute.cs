using LightenUp.Web.Data;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using LightenUp.Web.Models;

namespace LightenUp.Web.Filters;

/// <summary>Blocks patient premium features (worksheets, advanced stats) without active personal or company subscription.</summary>
// #Class RequiresPatientPremiumAttribute#
public class RequiresPatientPremiumAttribute : ActionFilterAttribute
{
    // #Function OnActionExecutionAsync#
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var access = context.HttpContext.RequestServices.GetRequiredService<SubscriptionAccessService>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.HttpContext.User);

        if (user == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (patient == null)
        {
            context.Result = new RedirectToActionResult("Welcome", "Onboarding", new { area = "Patient" });
            return;
        }

        if (!await access.HasPatientPremiumAccessAsync(patient))
        {
            context.Result = new RedirectToActionResult("Index", "Subscription", new { area = "Patient" });
            return;
        }

        await next();
    }
}
