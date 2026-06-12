using LightenUp.Web.Data;
using LightenUp.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

/// <summary>Activates assignments and snapshots payroll fields (SlotValue + revenue %).</summary>
public class AssignmentActivationService
{
    private readonly ApplicationDbContext _context;
    private readonly SubscriptionPricingService _pricing;

    public AssignmentActivationService(ApplicationDbContext context, SubscriptionPricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    public async Task ActivateAsync(
        PatientPsychologistAssignment assignment,
        string? decisionByUserId = null,
        string? decisionNote = null,
        decimal? psychologistRevenuePercentage = null)
    {
        var pricingResult = await _pricing.GetSlotValueForPatientAsync(assignment.PatientId);
        var pct = psychologistRevenuePercentage
            ?? await _pricing.GetDefaultPsychologistPercentageAsync(assignment.PsychologistId);

        assignment.Status = "Active";
        assignment.SlotValue = pricingResult.SlotValue;
        assignment.MaxSessionsPerMonth = pricingResult.MaxSessions;
        assignment.PsychologistRevenuePercentage = pct;

        if (decisionByUserId != null)
        {
            assignment.DecisionByUserId = decisionByUserId;
            assignment.DecisionAt = DateTime.UtcNow;
            assignment.DecisionNote = decisionNote;
        }
    }

    public async Task<bool> PatientHasBlockingAssignmentAsync(int patientId)
    {
        return await _context.Assignments.AnyAsync(a =>
            a.PatientId == patientId &&
            (a.Status == "Active" ||
             a.Status == "PendingPsychologistApproval" ||
             a.Status == "PendingAdminApproval" ||
             a.Status == "PendingCancellationByHr" ||
             a.Status == "PendingCancellationByAdmin"));
    }
}
