using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Auth;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Factories;

namespace PRM.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(PrmDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
            return;

        var utcNow = DateTime.UtcNow;
        var admin = UserFactory.CreateAdminBootstrap(passwordHasher.Hash("Admin@1234"), utcNow);
        context.Users.Add(admin);

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
