using MediatR;
using PRM.Application.Features.Allocations.Dtos;

namespace PRM.Application.Features.Allocations.Commands;

public sealed record CreateAllocationCommand(
    long ProjectId,
    long EmployeeId,
    decimal UtilisationPercentage,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<AllocationDto>;
