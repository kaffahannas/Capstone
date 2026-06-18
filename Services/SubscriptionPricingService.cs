using LightenUp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

public sealed class PatientPricingResult
{
    public decimal SubscriptionValuePerMonth { get; set; }
    public int MaxSessions { get; set; }
    public bool IsB2B { get; set; }
    /// <summary>Total company plan payment used for B2B allocation.</summary>
    public decimal? B2BPlanAmount { get; set; }
    /// <summary>Employee divisor: package quota or active headcount.</summary>
    public int? B2BEmployeeCount { get; set; }

    public decimal PerSessionValue => MaxSessions > 0
        ? Math.Round(SubscriptionValuePerMonth / MaxSessions, 2, MidpointRounding.AwayFromZero)
        : 0;
}

/// <summary>Resolves monthly subscription slot value (IDR) for payroll.</summary>
// #Class SubscriptionPricingService#
public class SubscriptionPricingService
{
    public const decimal DefaultPsychologistRevenuePercentage = 40m;

    private readonly ApplicationDbContext _context;

    public SubscriptionPricingService(ApplicationDbContext context)
    {
        _context = context;
    }

    // #Bagian Harga Pasien#
    // #Function GetPatientPricingAsync#
    public async Task<PatientPricingResult> GetPatientPricingAsync(int patientId)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
        if (patient == null)
            return new PatientPricingResult { MaxSessions = 4 };

        if (patient.CompanyId != null)
            return await GetB2BPricingAsync(patient.CompanyId.Value);

        var (slotValue, maxSessions) = await GetB2CSlotValueAsync(patientId);
        return new PatientPricingResult
        {
            SubscriptionValuePerMonth = slotValue,
            MaxSessions = maxSessions,
            IsB2B = false
        };
    }

    // #Function GetSlotValueForPatientAsync#
    public async Task<(decimal SlotValue, int MaxSessions)> GetSlotValueForPatientAsync(int patientId)
    {
        var pricing = await GetPatientPricingAsync(patientId);
        return (pricing.SubscriptionValuePerMonth, pricing.MaxSessions);
    }

    // #Function GetB2CSlotValueAsync#
    public async Task<(decimal SlotValue, int MaxSessions)> GetB2CSlotValueAsync(int patientId)
    {
        var sub = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.PatientId == patientId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (sub == null)
            return (0, 4);

        var amount = await GetLinkedPaymentAmountAsync(sub.SubscriptionId, companySubscriptionId: null);
        return (amount, sub.MaxSessionsPerMonth);
    }

    // #Bagian Harga B2B#
    // #Function GetB2BPricingAsync#
    public async Task<PatientPricingResult> GetB2BPricingAsync(int companyId)
    {
        var companySub = await _context.CompanySubscriptions
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (companySub == null)
            return new PatientPricingResult { MaxSessions = 4, IsB2B = true };

        var planAmount = await GetLinkedPaymentAmountAsync(
            subscriptionId: null,
            companySubscriptionId: companySub.CompanySubscriptionId);
        var employeeCount = await ResolveB2BEmployeeCountAsync(companyId, companySub.EmployeeLimit);

        if (planAmount <= 0 || employeeCount <= 0)
        {
            return new PatientPricingResult
            {
                MaxSessions = companySub.MaxSessionsPerMonth,
                IsB2B = true,
                B2BPlanAmount = planAmount,
                B2BEmployeeCount = employeeCount
            };
        }

        var perEmployeeMonthly = Math.Round(planAmount / employeeCount, 2, MidpointRounding.AwayFromZero);
        return new PatientPricingResult
        {
            SubscriptionValuePerMonth = perEmployeeMonthly,
            MaxSessions = companySub.MaxSessionsPerMonth,
            IsB2B = true,
            B2BPlanAmount = planAmount,
            B2BEmployeeCount = employeeCount
        };
    }

    // #Function GetB2BSlotValueAsync#
    public async Task<(decimal SlotValue, int MaxSessions)> GetB2BSlotValueAsync(int companyId)
    {
        var pricing = await GetB2BPricingAsync(companyId);
        return (pricing.SubscriptionValuePerMonth, pricing.MaxSessions);
    }

    private async Task<decimal> GetLinkedPaymentAmountAsync(int? subscriptionId, int? companySubscriptionId)
    {
        var query = _context.PaymentTransactions.AsNoTracking().AsQueryable();

        if (subscriptionId.HasValue)
            query = query.Where(p => p.SubscriptionId == subscriptionId.Value);
        else if (companySubscriptionId.HasValue)
            query = query.Where(p => p.CompanySubscriptionId == companySubscriptionId.Value);
        else
            return 0;

        var paid = await query
            .Where(p => p.PaymentStatus == "paid")
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .Select(p => (decimal?)p.Amount)
            .FirstOrDefaultAsync();

        if (paid.GetValueOrDefault() > 0)
            return paid!.Value;

        // Active subscription implies checkout completed; during local dev payment may still be "pending".
        var linked = await query
            .Where(p => p.Amount > 0)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (decimal?)p.Amount)
            .FirstOrDefaultAsync();

        return linked ?? 0;
    }

    private async Task<int> ResolveB2BEmployeeCountAsync(int companyId, int packageEmployeeLimit)
    {
        if (packageEmployeeLimit > 0)
            return packageEmployeeLimit;

        var activeEmployees = await _context.Patients.CountAsync(p =>
            p.CompanyId == companyId && p.EmploymentStatus == "active");
        activeEmployees += await _context.PendingEmployees.CountAsync(p => p.CompanyId == companyId);

        return activeEmployees > 0 ? activeEmployees : 1;
    }

    // #Bagian Payroll#
    // #Function GetDefaultPsychologistPercentageAsync#
    public async Task<decimal> GetDefaultPsychologistPercentageAsync(int psychologistId)
    {
        var setting = await _context.PayrollSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PsychologistId == psychologistId);

        return setting?.PsychologistPercentage ?? DefaultPsychologistRevenuePercentage;
    }
}
