using MediatR;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Projects.Commands;

public sealed record UpdateMilestoneStatusCommand(
    long ProjectId,
    long MilestoneId,
    MilestoneStatus Status) : IRequest;
