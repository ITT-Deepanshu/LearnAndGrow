using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Domain.Factories;

public static class UserFactory
{
    public static User CreateAdminBootstrap(string passwordHash, DateTime utcNow) =>
        User.Create("admin", "admin@prm.local", "System Administrator", passwordHash, UserRole.Admin, 0, utcNow, forcePasswordChange: true);

    public static User CreateAccount(
        string username,
        string email,
        string fullName,
        string passwordHash,
        UserRole role,
        long createdBy,
        DateTime utcNow) =>
        User.Create(username, email, fullName, passwordHash, role, createdBy, utcNow, forcePasswordChange: true);
}
