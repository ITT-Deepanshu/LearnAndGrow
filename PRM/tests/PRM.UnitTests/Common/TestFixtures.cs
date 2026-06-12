using System.Reflection;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
namespace PRM.UnitTests.Common;

internal static class TestFixtures
{
    public static readonly DateTime FixedUtc = new(2026, 5, 14, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly FixedToday = new(2026, 5, 14);

    public static void SetId(object entity, long id)
    {
        var type = entity.GetType();
        while (type is not null)
        {
            var prop = type.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop is not null)
            {
                prop.SetValue(entity, id);
                return;
            }
            type = type.BaseType;
        }
    }

    public static void SetProperty(object entity, string name, object? value)
    {
        var prop = entity.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(entity, value);
    }

    public static User CreateUser(
        UserRole role = UserRole.Manager,
        bool isActive = true,
        string passwordHash = "hash",
        long id = 1)
    {
        var user = User.Create("test.user", "test@prm.local", passwordHash, role, 0, FixedUtc, requiresPasswordChange: true);
        SetId(user, id);
        SetProperty(user, "Role", Role.Create((long)role, role.ToString().ToLowerInvariant(), "Test role"));
        if (!isActive) user.Deactivate(0, FixedUtc);
        return user;
    }

    public static ResourceProfile CreateResourceProfile(long userId = 2, long id = 10, ResourceProfileStatus status = ResourceProfileStatus.Bench)
    {
        var profile = ResourceProfile.Create(userId, "Test User", "Backend", "Developer", 1, FixedUtc);
        SetId(profile, id);
        var user = CreateUser(UserRole.Resource, id: userId);
        SetProperty(profile, "User", user);
        profile.RecomputeStatus(status switch
        {
            ResourceProfileStatus.Bench => 0,
            ResourceProfileStatus.PartiallyAllocated => 50,
            ResourceProfileStatus.Allocated => 100,
            _ => 0
        });
        return profile;
    }

    public static Project CreateProject(
        long managerId = 1,
        ProjectStatus status = ProjectStatus.Active,
        long id = 201)
    {
        var project = Project.Create(
            "Alpha Portal",
            "Test project",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30),
            status,
            managerId,
            120,
            1,
            FixedUtc);
        SetId(project, id);
        return project;
    }

    public static Allocation CreateAllocation(
        long resourceProfileId,
        long projectId,
        decimal utilisation,
        DateOnly from,
        DateOnly to,
        long id = 100)
    {
        var allocation = Allocation.Create(resourceProfileId, projectId, utilisation, from, to, 1, FixedUtc);
        SetId(allocation, id);
        return allocation;
    }
}
