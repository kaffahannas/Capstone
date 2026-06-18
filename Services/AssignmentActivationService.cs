using LightenUp.Web.Data;
using LightenUp.Web.Models;
using LightenUp.Web.Models.Constants;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

/// <summary>Activates assignments and snapshots payroll fields (SlotValue + revenue %).</summary>
// #Class AssignmentActivationService#
public class AssignmentActivationService
{
    private readonly ApplicationDbContext _context;
    private readonly SubscriptionPricingService _pricing;

    public static readonly string[] LiveClientListStatuses =
    {
        AssignmentStatus.Active,
        AssignmentStatus.PendingCancellationByHr,
        AssignmentStatus.PendingCancellationByAdmin
    };

    public AssignmentActivationService(ApplicationDbContext context, SubscriptionPricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    public static bool IsLiveClientListStatus(string status) =>
        status == AssignmentStatus.Active ||
        status == AssignmentStatus.PendingCancellationByHr ||
        status == AssignmentStatus.PendingCancellationByAdmin;

    public static int LiveAssignmentPriority(string status) => status switch
    {
        AssignmentStatus.Active => 0,
        AssignmentStatus.PendingCancellationByHr => 1,
        AssignmentStatus.PendingCancellationByAdmin => 1,
        _ => 2
    };

    public static PatientPsychologistAssignment SelectPrimaryAssignment(IEnumerable<PatientPsychologistAssignment> assignments) =>
        assignments
            .OrderBy(a => LiveAssignmentPriority(a.Status))
            .ThenByDescending(a => a.AssignedAt)
            .First();

    public static List<PatientPsychologistAssignment> SelectPrimaryPerPatient(IEnumerable<PatientPsychologistAssignment> assignments) =>
        assignments
            .GroupBy(a => a.PatientId)
            .Select(SelectPrimaryAssignment)
            .ToList();

    // #Function ActivateAsync#
    public async Task ActivateAsync(
        PatientPsychologistAssignment assignment,
        string? decisionByUserId = null,
        string? decisionNote = null,
        decimal? psychologistRevenuePercentage = null)
    {
        await SupersedeConflictingAssignmentsAsync(assignment, decisionByUserId);

        var pricingResult = await _pricing.GetSlotValueForPatientAsync(assignment.PatientId);
        var pct = psychologistRevenuePercentage
            ?? await _pricing.GetDefaultPsychologistPercentageAsync(assignment.PsychologistId);

        assignment.Status = AssignmentStatus.Active;
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

    // #Function SupersedeConflictingAssignmentsAsync#
    public async Task SupersedeConflictingAssignmentsAsync(
        PatientPsychologistAssignment keep,
        string? decisionByUserId = null,
        string? reason = null)
    {
        var others = await _context.Assignments
            .Where(a => a.PatientId == keep.PatientId &&
                        a.PsychologistId == keep.PsychologistId &&
                        a.AssignmentId != keep.AssignmentId &&
                        (a.Status == AssignmentStatus.Active ||
                         a.Status == AssignmentStatus.PendingPsychologistApproval ||
                         a.Status == AssignmentStatus.PendingAdminApproval ||
                         a.Status == AssignmentStatus.PendingCancellationByHr ||
                         a.Status == AssignmentStatus.PendingCancellationByAdmin))
            .ToListAsync();

        if (others.Count == 0) return;

        var note = reason ?? "Digantikan oleh penugasan baru.";
        var now = DateTime.UtcNow;
        foreach (var other in others)
        {
            other.Status = AssignmentStatus.Cancelled;
            other.DecisionNote = note;
            other.DecisionAt = now;
            other.DecisionByUserId = decisionByUserId;
        }
    }

    // #Function RepairDuplicateLiveAssignmentsAsync#
    public async Task RepairDuplicateLiveAssignmentsAsync(int psychologistId, string? decisionByUserId = null)
    {
        var live = await _context.Assignments
            .Where(a => a.PsychologistId == psychologistId &&
                        (a.Status == AssignmentStatus.Active ||
                         a.Status == AssignmentStatus.PendingCancellationByHr ||
                         a.Status == AssignmentStatus.PendingCancellationByAdmin))
            .ToListAsync();

        var duplicateGroups = live.GroupBy(a => a.PatientId).Where(g => g.Count() > 1);
        var changed = false;
        var now = DateTime.UtcNow;

        foreach (var group in duplicateGroups)
        {
            var primary = SelectPrimaryAssignment(group);
            foreach (var duplicate in group.Where(a => a.AssignmentId != primary.AssignmentId))
            {
                duplicate.Status = AssignmentStatus.Cancelled;
                duplicate.DecisionNote = "Penugasan duplikat ditutup otomatis.";
                duplicate.DecisionAt = now;
                duplicate.DecisionByUserId = decisionByUserId;
                changed = true;
            }
        }

        if (changed)
            await _context.SaveChangesAsync();
    }

    public async Task<bool> PatientHasBlockingAssignmentAsync(int patientId)
    {
        return await _context.Assignments.AnyAsync(a =>
            a.PatientId == patientId &&
            (a.Status == AssignmentStatus.Active ||
             a.Status == AssignmentStatus.PendingPsychologistApproval ||
             a.Status == AssignmentStatus.PendingAdminApproval ||
             a.Status == AssignmentStatus.PendingCancellationByHr ||
             a.Status == AssignmentStatus.PendingCancellationByAdmin));
    }

    public async Task<bool> HasLiveAssignmentForPairAsync(int patientId, int psychologistId)
    {
        return await _context.Assignments.AnyAsync(a =>
            a.PatientId == patientId &&
            a.PsychologistId == psychologistId &&
            (a.Status == AssignmentStatus.Active ||
             a.Status == AssignmentStatus.PendingPsychologistApproval ||
             a.Status == AssignmentStatus.PendingAdminApproval ||
             a.Status == AssignmentStatus.PendingCancellationByHr ||
             a.Status == AssignmentStatus.PendingCancellationByAdmin));
    }
}
