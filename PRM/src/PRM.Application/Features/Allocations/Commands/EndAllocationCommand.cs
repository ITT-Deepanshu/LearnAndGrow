using MediatR;

namespace PRM.Application.Features.Allocations.Commands;

public sealed record EndAllocationCommand(long AllocationId) : IRequest;
