using MediatR;
using PRM.Application.Features.Allocations.Dtos;

namespace PRM.Application.Features.Allocations.Queries;

public sealed record ListAllocationsByEmployeeQuery(long EmployeeId) : IRequest<IReadOnlyList<AllocationDto>>;
