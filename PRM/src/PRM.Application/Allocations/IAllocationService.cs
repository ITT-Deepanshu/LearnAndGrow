using PRM.Application.Allocations;

namespace PRM.Application.Allocations;

public interface IAllocationService
{
    Task<AllocationDto> CreateAllocationAsync(CreateAllocationDto dto, CancellationToken cancellationToken = default);
    Task EndAllocationAsync(long allocationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationListItemDto>> ListAllocationsAsync(long? resourceProfileId, long? projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationDto>> ListAllocationsByProjectAsync(long projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationDto>> ListAllocationsByEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllocationDto>> ListMyAllocationsAsync(CancellationToken cancellationToken = default);
}
