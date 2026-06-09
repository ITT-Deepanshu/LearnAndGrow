using MediatR;
using PRM.Application.Features.Allocations.Dtos;

namespace PRM.Application.Features.Allocations.Queries;

public sealed record ListAllocationsQuery(long? EmployeeId, long? ProjectId) : IRequest<IReadOnlyList<AllocationListItemDto>>;
