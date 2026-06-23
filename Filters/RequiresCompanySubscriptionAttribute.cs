using LightenUp.Web.Data;
using LightenUp.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Filters;

/// <summary>Blocks HR feature actions when the company has no active B2B subscription.</summary>
// #Class RequiresCompanySubscriptionAttribute#
public class RequiresCompanySubscriptionAttribute : ActionFilterAttribute
{
    // #Function OnActionExecutionAsync#
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var access = context.HttpContext.RequestServices.GetRequiredService<SubscriptionAccessService>();
        var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        var hr = await db.HrStaffs.FirstOrDefaultAsync(h => h.UserId == userId);
        if (hr?.CompanyId == null)
        {
            context.Result = new RedirectToActionResult("Company", "Onboarding", new { area = "Hr" });
            return;
        }

        if (!await access.HasCompanyActiveSubscriptionAsync(hr.CompanyId.Value))
        {
            if (context.Controller is Controller controller)
                controller.TempData["subscriptionRequired"] = true;
            context.Result = new RedirectToActionResult("Index", "Subscription", new { area = "Hr" });
            return;
        }

        await next();
    }
}
