using MediatR;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Projects.Commands;

public sealed record UpdateProjectCommand(
    long ProjectId,
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    long ManagerId,
    int TotalStoryPoints) : IRequest;
