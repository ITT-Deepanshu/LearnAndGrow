using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Projects.Commands;

public sealed record CreateProjectCommand(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    ProjectStatus Status,
    long ManagerId,
    int TotalStoryPoints) : IRequest<ProjectDto>;
