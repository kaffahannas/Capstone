using LightenUp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace LightenUp.Web.Services;

public class PsychologistWorkloadInfo
{
    public int PsychologistId { get; set; }
    public string FullName { get; set; } = "";
    public string? Specialization { get; set; }
    public int ActiveCaseload { get; set; }
    public int B2BCaseload { get; set; }
    public int PublicCaseload { get; set; }
    public string WorkloadLevel { get; set; } = "normal"; // low | normal | high
}

// #Class PsychologistWorkloadService#
public class PsychologistWorkloadService
{
    private readonly ApplicationDbContext _context;

    public PsychologistWorkloadService(ApplicationDbContext context)
    {
        _context = context;
    }

    // #Bagian Beban Kerja#
    // #Function GetActiveCaseloadCountsAsync#
    public async Task<(Dictionary<int, int> Total, Dictionary<int, int> B2B, Dictionary<int, int> Public)> GetActiveCaseloadCountsAsync()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Patient)
            .Where(a => a.Status == "Active")
            .ToListAsync();

        var total = assignments.GroupBy(a => a.PsychologistId)
            .ToDictionary(g => g.Key, g => g.Count());
            
        var b2b = assignments.Where(a => a.Patient?.CompanyId != null)
            .GroupBy(a => a.PsychologistId)
            .ToDictionary(g => g.Key, g => g.Count());
            
        var pub = assignments.Where(a => a.Patient?.CompanyId == null)
            .GroupBy(a => a.PsychologistId)
            .ToDictionary(g => g.Key, g => g.Count());

        return (total, b2b, pub);
    }

    // #Function GetApprovedPsychologistsWithWorkloadAsync#
    public async Task<List<PsychologistWorkloadInfo>> GetApprovedPsychologistsWithWorkloadAsync(bool b2bOnly = false)
    {
        var (total, b2b, pub) = await GetActiveCaseloadCountsAsync();

        var query = _context.Psychologists
            .Include(p => p.User)
            .Where(p => p.User != null && p.User.IsApprovedByAdmin);

        if (b2bOnly)
            query = query.Where(p => p.AcceptsB2B);

        var psychologists = await query.OrderBy(p => p.User!.FullName).ToListAsync();

        return psychologists.Select(p =>
        {
            total.TryGetValue(p.PsychologistId, out var count);
            b2b.TryGetValue(p.PsychologistId, out var countB2B);
            pub.TryGetValue(p.PsychologistId, out var countPub);
            
            return new PsychologistWorkloadInfo
            {
                PsychologistId = p.PsychologistId,
                FullName = p.User?.FullName ?? "—",
                Specialization = p.Specialization,
                ActiveCaseload = count,
                B2BCaseload = countB2B,
                PublicCaseload = countPub,
                WorkloadLevel = count < 5 ? "low" : (count > 15 ? "high" : "normal")
            };
        }).ToList();
    }

    // #Function WorkloadLabel#
    public static string WorkloadLabel(string level) => level switch
    {
        "low" => "Ringan (<5)",
        "high" => "Tinggi (>15)",
        _ => "Normal"
    };
}
