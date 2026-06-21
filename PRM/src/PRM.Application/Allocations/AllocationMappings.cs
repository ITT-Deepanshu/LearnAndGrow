using PRM.Application.Allocations;
using PRM.Domain.Entities;

namespace PRM.Application.Allocations;

internal static class AllocationMappings
{
    internal static AllocationDto ToDto(Allocation allocation) =>
        new(
            allocation.Id,
            allocation.ResourceProfileId,
            allocation.ResourceProfile?.FullName ?? string.Empty,
            allocation.ProjectId,
            allocation.Project?.Name ?? string.Empty,
            allocation.UtilisationPercentage,
            allocation.FromDate,
            allocation.ToDate,
            allocation.EndedAt);

    internal static AllocationListItemDto ToListItemDto(Allocation allocation) =>
        new(
            allocation.Id,
            allocation.ResourceProfileId,
            allocation.ResourceProfile?.FullName ?? string.Empty,
            allocation.ProjectId,
            allocation.Project?.Name ?? string.Empty,
            allocation.UtilisationPercentage,
            allocation.FromDate,
            allocation.ToDate);
}
