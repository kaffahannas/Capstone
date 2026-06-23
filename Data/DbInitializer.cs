using LightenUp.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace LightenUp.Web.Data;

/// <summary>Entry point for domain seed data — see <see cref="DummyDataSeed"/>.</summary>
// #Class DbInitializer#
public static class DbInitializer
{
    // #Function SeedAsync#
    public static Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        => DummyDataSeed.SeedAsync(context, userManager);
}
