using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Auth;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PrmDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken)
            || await context.ActivityTags.AnyAsync(cancellationToken))
            return;

        var utcNow = DateTime.UtcNow;
        var admin = User.Create("admin", "admin@prm.local", passwordHasher.Hash("Admin@1234"), UserRole.Admin, 0, utcNow, requiresPasswordChange: true);
        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        context.ResourceProfiles.Add(ResourceProfile.Create(
            admin.Id,
            "System Administrator",
            string.Empty,
            string.Empty,
            0,
            utcNow));

        context.SystemConfigurations.Add(SystemConfiguration.CreateDefault(0, utcNow));

        var tags = new[]
        {
            "Backend API Development",
            "Microservices / Architecture",
            "Database Design & Queries",
            "WebSocket / Real-time Features",
            "Frontend Development",
            "Code Review / Mentoring",
            "Bug Fixing",
            "DevOps / Deployment",
            "Testing & QA",
            "Documentation",
            "Other"
        };

        for (var i = 0; i < tags.Length; i++)
            context.ActivityTags.Add(ActivityTag.Create(i + 1, tags[i], tags[i] == "Other"));

        await context.SaveChangesAsync(cancellationToken);
    }
}
