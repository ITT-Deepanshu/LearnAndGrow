using MediatR;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed record UpdateMaxWeeklyHoursCommand(int Hours) : IRequest;
