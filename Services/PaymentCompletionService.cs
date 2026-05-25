using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

public static class PaymentCompletionService
{
    public static async Task MarkPaidAsync(ApplicationDbContext context, PaymentTransaction payment)
    {
        if (payment.PaymentStatus == "paid") return;

        payment.PaymentStatus = "paid";
        payment.PaidAt = DateTime.UtcNow;

        if (payment.SubscriptionId.HasValue)
        {
            var sub = payment.Subscription ?? await context.Subscriptions.FindAsync(payment.SubscriptionId.Value);
            if (sub != null)
            {
                sub.Status = "Active";
                sub.StartDate = DateTime.Today;
                sub.EndDate = DateTime.Today.AddMonths(sub.PlanName.Contains("Tahunan", StringComparison.OrdinalIgnoreCase) ? 12 : 1);
            }
        }

        if (payment.CompanySubscriptionId.HasValue)
        {
            var companySub = payment.CompanySubscription
                ?? await context.CompanySubscriptions
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.CompanySubscriptionId == payment.CompanySubscriptionId.Value);

            if (companySub != null)
            {
                companySub.Status = "Active";
                companySub.StartDate = DateTime.Today;
                companySub.EndDate = DateTime.Today.AddMonths(
                    companySub.PlanName.Contains("Tahunan", StringComparison.OrdinalIgnoreCase) ? 12 : 1);

                var company = companySub.Company
                    ?? await context.Companies.FindAsync(companySub.CompanyId);
                if (company != null)
                {
                    bool hasDivisions = await context.CompanyDivisions.AnyAsync(d => d.CompanyId == company.CompanyId);
                    if (!hasDivisions)
                    {
                        var access = new SubscriptionAccessService(context);
                        var newDiv = new CompanyDivision
                        {
                            CompanyId = company.CompanyId,
                            Name = "Pusat",
                            ReferralCode = await access.GenerateUniqueReferralCodeAsync()
                        };
                        context.CompanyDivisions.Add(newDiv);
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
