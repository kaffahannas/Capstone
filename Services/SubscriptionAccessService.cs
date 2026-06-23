using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

public class SubscriptionAccessService
{
    private readonly ApplicationDbContext _context;

    public SubscriptionAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasCompanyActiveSubscriptionAsync(int companyId)
    {
        return await _context.CompanySubscriptions
            .AnyAsync(s => s.CompanyId == companyId
                && s.Status == "Active"
                && s.EndDate >= DateTime.Today);
    }

    public async Task<CompanySubscription?> GetActiveCompanySubscriptionAsync(int companyId)
    {
        return await _context.CompanySubscriptions
            .Where(s => s.CompanyId == companyId && s.Status == "Active" && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasPatientPremiumAccessAsync(Patient patient)
    {
        // Sumber 1: B2C — punya langganan psikolog pribadi aktif
        var hasOwn = await _context.Subscriptions
            .AnyAsync(s => s.PatientId == patient.PatientId
                && s.Status == "Active"
                && s.EndDate >= DateTime.Today);
        if (hasOwn) return true;

        // Sumber 2: B2B — ditanggung perusahaan (HR per-seat)
        if (patient.CompanyId != null)
            return await HasCompanyActiveSubscriptionAsync(patient.CompanyId.Value);

        // Sumber 3: Pasien klinik Mitra — ditanggung psikolog Mitra
        if (patient.SponsorPsychologistId != null)
            return await HasPsychologistMitraActiveAsync(patient.SponsorPsychologistId.Value);

        return false;
    }

    public async Task<bool> HasPsychologistMitraActiveAsync(int psychologistId)
    {
        return await _context.PsychologistSubscriptions
            .AnyAsync(s => s.PsychologistId == psychologistId
                && s.Status == "Active"
                && s.EndDate >= DateTime.Today);
    }

    public async Task<PsychologistSubscription?> GetActivePsychologistSubscriptionAsync(int psychologistId)
    {
        return await _context.PsychologistSubscriptions
            .Where(s => s.PsychologistId == psychologistId
                && s.Status == "Active"
                && s.EndDate >= DateTime.Today)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanUseReferralCodeAsync(int companyId)
    {
        return await HasCompanyActiveSubscriptionAsync(companyId);
    }

    public static string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
    }

    public async Task<string> GenerateUniqueReferralCodeAsync()
    {
        string code;
        do { code = GenerateReferralCode(); }
        while (await _context.CompanyDivisions.AnyAsync(c => c.ReferralCode == code)
            || await _context.Psychologists.AnyAsync(p => p.MitraReferralCode == code));
        return code;
    }
}
