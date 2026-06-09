using PRM.Application.Features.Allocations.Dtos;
using PRM.Domain.Entities;

namespace PRM.Application.Features.Allocations;

internal static class AllocationMappings
{
    internal static AllocationDto ToDto(Allocation allocation) =>
        new(
            allocation.Id,
            allocation.EmployeeId,
            allocation.Employee?.User?.FullName ?? string.Empty,
            allocation.ProjectId,
            allocation.Project?.Name ?? string.Empty,
            allocation.UtilisationPercentage,
            allocation.FromDate,
            allocation.ToDate,
            allocation.EndedAt);

    internal static AllocationListItemDto ToListItemDto(Allocation allocation) =>
        new(
            allocation.Id,
            allocation.EmployeeId,
            allocation.Employee?.User?.FullName ?? string.Empty,
            allocation.ProjectId,
            allocation.Project?.Name ?? string.Empty,
            allocation.UtilisationPercentage,
            allocation.FromDate,
            allocation.ToDate);
}
