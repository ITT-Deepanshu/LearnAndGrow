using PRM.Application.Interfaces.Common;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Common;

/// <summary>
/// Shared authorization checks used by application services.
/// Controllers still use [Authorize]; these guard business operations.
/// </summary>
public static class CurrentUserGuards
{
    public static void EnsureAuthenticated(ICurrentUser user)
    {
        if (user.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
    }

    public static void EnsureRole(ICurrentUser user, params UserRole[] allowedRoles)
    {
        EnsureAuthenticated(user);
        if (user.Role is null || !allowedRoles.Contains(user.Role.Value))
            throw new ForbiddenException("Insufficient permissions.");
    }

    public static void EnsureAdmin(ICurrentUser user) =>
        EnsureRole(user, UserRole.Admin);

    public static void EnsureManager(ICurrentUser user) =>
        EnsureRole(user, UserRole.Manager);

    public static void EnsureResource(ICurrentUser user) =>
        EnsureRole(user, UserRole.Resource);

    public static long ActorId(ICurrentUser user)
    {
        EnsureAuthenticated(user);
        return user.UserId!.Value;
    }

    public static long? ManagerFilter(ICurrentUser user)
    {
        EnsureAuthenticated(user);
        return user.Role switch
        {
            UserRole.Admin => null,
            UserRole.Manager => user.UserId,
            _ => throw new ForbiddenException("Insufficient permissions.")
        };
    }

    public static void EnsureCanViewProject(ICurrentUser user, long projectManagerId)
    {
        EnsureAuthenticated(user);
        if (user.Role == UserRole.Admin)
            return;
        if (user.Role == UserRole.Manager && user.UserId == projectManagerId)
            return;
        throw new ForbiddenException("You do not have access to this project.");
    }

    public static void EnsureOwnsProject(ICurrentUser user, long projectManagerId)
    {
        EnsureManager(user);
        if (user.UserId != projectManagerId)
            throw new ForbiddenException("You do not own this project.");
    }

    /// <summary>
    /// Managers may view direct reports, or any active employee they can allocate to a project.
    /// </summary>
    public static void EnsureCanViewEmployee(ICurrentUser user, long? employeeManagerId, ResourceProfileStatus status)
    {
        EnsureAuthenticated(user);
        if (user.Role == UserRole.Admin)
            return;

        if (user.Role == UserRole.Manager)
        {
            if (employeeManagerId == user.UserId)
                return;

            if (status != ResourceProfileStatus.Inactive)
                return;
        }

        throw new ForbiddenException("You do not have access to this resource profile.");
    }
}
