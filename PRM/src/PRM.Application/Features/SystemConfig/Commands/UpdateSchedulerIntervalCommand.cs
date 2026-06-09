using MediatR;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed record UpdateSchedulerIntervalCommand(int Minutes) : IRequest;
