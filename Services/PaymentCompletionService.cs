using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

// #Class PaymentCompletionService#
public static class PaymentCompletionService
{
    // #Function MarkPaidAsync#
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
                var existingActive = await context.Subscriptions
                    .Where(s => s.PatientId == sub.PatientId && s.Status == "Active" && s.EndDate > DateTime.Today && s.SubscriptionId != sub.SubscriptionId)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                var startDate = existingActive != null ? existingActive.EndDate : DateTime.Today;
                var durationMonths = sub.PlanName.Contains("Tahunan", StringComparison.OrdinalIgnoreCase) ? 12 : 1;

                sub.Status = "Active";
                sub.StartDate = startDate;
                sub.EndDate = startDate.AddMonths(durationMonths);
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
                var existingActive = await context.CompanySubscriptions
                    .Where(s => s.CompanyId == companySub.CompanyId && s.Status == "Active" && s.EndDate > DateTime.Today && s.CompanySubscriptionId != companySub.CompanySubscriptionId)
                    .OrderByDescending(s => s.EndDate)
                    .FirstOrDefaultAsync();

                var startDate = existingActive != null ? existingActive.EndDate : DateTime.Today;
                var durationMonths = companySub.PlanName.Contains("Tahunan", StringComparison.OrdinalIgnoreCase) ? 12 : 1;

                companySub.Status = "Active";
                companySub.StartDate = startDate;
                companySub.EndDate = startDate.AddMonths(durationMonths);

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
