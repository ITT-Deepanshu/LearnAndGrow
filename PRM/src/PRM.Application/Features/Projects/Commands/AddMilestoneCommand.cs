using MediatR;
using PRM.Application.Features.Projects.Dtos;

namespace PRM.Application.Features.Projects.Commands;

public sealed record AddMilestoneCommand(
    long ProjectId,
    string Title,
    DateOnly DueDate,
    int StoryPoints) : IRequest<MilestoneDto>;
