using PRM.Domain.Constants;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Application.Auth;

public static class UserPermissionMapper
{
    public static IReadOnlyList<string> GetPermissions(User user)
    {
        var fromDb = user.Role.Permissions
            .Select(p => p.Permission)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (fromDb.Count > 0)
            return fromDb;

        return user.Role.RoleName.ToLowerInvariant() switch
        {
            "admin" => GetPermissionsForRole(UserRole.Admin),
            "manager" => GetPermissionsForRole(UserRole.Manager),
            "resource" => GetPermissionsForRole(UserRole.Resource),
            _ => []
        };
    }

    public static IReadOnlyList<string> GetPermissionsForRole(UserRole role) =>
        role switch
        {
            UserRole.Admin =>
            [
                RolePermissions.UsersManage,
                RolePermissions.ResourceProfilesManage,
                RolePermissions.ProjectsManage,
                RolePermissions.AllocationsViewAll,
                RolePermissions.SystemManage
            ],
            UserRole.Manager =>
            [
                RolePermissions.DashboardView,
                RolePermissions.AllocationsManage,
                RolePermissions.ProjectsViewOwn,
                RolePermissions.TimesheetsViewTeam,
                RolePermissions.AiUse
            ],
            UserRole.Resource =>
            [
                RolePermissions.TimesheetsSubmit,
                RolePermissions.TimesheetsViewOwn,
                RolePermissions.AllocationsViewOwn
            ],
            _ => []
        };
}
