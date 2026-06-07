using LightenUp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

/// <summary>Resolves monthly subscription slot value (IDR) for payroll.</summary>
public class SubscriptionPricingService
{
    public const decimal DefaultPsychologistRevenuePercentage = 40m;

    private readonly ApplicationDbContext _context;

    public SubscriptionPricingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetSlotValueForPatientAsync(int patientId)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
        if (patient == null) return 0;

        if (patient.CompanyId != null)
            return await GetB2BSlotValueAsync(patient.CompanyId.Value);

        return await GetB2CSlotValueAsync(patientId);
    }

    public async Task<decimal> GetB2CSlotValueAsync(int patientId)
    {
        var payment = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.PatientId == patientId && p.PaymentStatus == "paid")
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

        if (payment != null) return payment.Amount;

        var sub = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.PatientId == patientId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (sub == null) return 0;

        var subPayment = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.SubscriptionId == sub.SubscriptionId && p.PaymentStatus == "paid")
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

        return subPayment?.Amount ?? 0;
    }

    public async Task<decimal> GetB2BSlotValueAsync(int companyId)
    {
        var companySub = await _context.CompanySubscriptions
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (companySub == null) return 0;

        var payment = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.CompanySubscriptionId == companySub.CompanySubscriptionId && p.PaymentStatus == "paid")
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

        var planAmount = payment?.Amount ?? 0;
        if (planAmount <= 0) return 0;

        var limit = companySub.EmployeeLimit;
        if (limit <= 0) limit = 1;

        return Math.Round(planAmount / limit, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<decimal> GetDefaultPsychologistPercentageAsync(int psychologistId)
    {
        var setting = await _context.PayrollSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PsychologistId == psychologistId);

        return setting?.PsychologistPercentage ?? DefaultPsychologistRevenuePercentage;
    }
}
