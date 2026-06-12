using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Application.Common;

public static class AllocationEligibility
{
    public static bool IsEligibleActiveResource(ResourceProfile profile) =>
        profile.User.IsActive
        && profile.User.AppRole == UserRole.Resource
        && profile.Status != ResourceProfileStatus.Inactive;
}
