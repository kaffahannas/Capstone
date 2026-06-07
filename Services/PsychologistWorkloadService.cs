using LightenUp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

public class PsychologistWorkloadInfo
{
    public int PsychologistId { get; set; }
    public string FullName { get; set; } = "";
    public string? Specialization { get; set; }
    public int ActiveCaseload { get; set; }
    public string WorkloadLevel { get; set; } = "normal"; // low | normal | high
}

public class PsychologistWorkloadService
{
    private readonly ApplicationDbContext _context;

    public PsychologistWorkloadService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, int>> GetActiveCaseloadCountsAsync()
    {
        return await _context.Assignments
            .Where(a => a.Status == "Active")
            .GroupBy(a => a.PsychologistId)
            .Select(g => new { PsyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PsyId, x => x.Count);
    }

    public async Task<List<PsychologistWorkloadInfo>> GetApprovedPsychologistsWithWorkloadAsync(bool b2bOnly = false)
    {
        var caseloads = await GetActiveCaseloadCountsAsync();

        var query = _context.Psychologists
            .Include(p => p.User)
            .Where(p => p.User != null && p.User.IsApprovedByAdmin);

        if (b2bOnly)
            query = query.Where(p => p.AcceptsB2B);

        var psychologists = await query.OrderBy(p => p.User!.FullName).ToListAsync();

        return psychologists.Select(p =>
        {
            caseloads.TryGetValue(p.PsychologistId, out var count);
            return new PsychologistWorkloadInfo
            {
                PsychologistId = p.PsychologistId,
                FullName = p.User?.FullName ?? "—",
                Specialization = p.Specialization,
                ActiveCaseload = count,
                WorkloadLevel = count < 5 ? "low" : (count > 15 ? "high" : "normal")
            };
        }).ToList();
    }

    public static string WorkloadLabel(string level) => level switch
    {
        "low" => "Ringan (<5)",
        "high" => "Tinggi (>15)",
        _ => "Normal"
    };
}
