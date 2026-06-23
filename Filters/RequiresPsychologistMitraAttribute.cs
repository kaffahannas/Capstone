using LightenUp.Web.Data;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using LightenUp.Web.Models;

namespace LightenUp.Web.Filters;

/// <summary>Blocks Mitra features when the psychologist does not have an active Mitra subscription.</summary>
public class RequiresPsychologistMitraAttribute : ActionFilterAttribute
{
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

        var psy = await db.Psychologists.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (psy == null)
        {
            context.Result = new RedirectToActionResult("Index", "Dashboard", new { area = "Psychologist" });
            return;
        }

        if (!await access.HasPsychologistMitraActiveAsync(psy.PsychologistId))
        {
            if (context.Controller is Controller controller)
                controller.TempData["mitraRequired"] = true;
            context.Result = new RedirectToActionResult("Index", "Mitra", new { area = "Psychologist" });
            return;
        }

        await next();
    }
}
