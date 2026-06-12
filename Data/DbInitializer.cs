using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Data;

/// <summary>Entry point for domain seed data — see <see cref="DummyDataSeed"/>.</summary>
public static class DbInitializer
{
    public static Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        => DummyDataSeed.SeedAsync(context, userManager);
}
