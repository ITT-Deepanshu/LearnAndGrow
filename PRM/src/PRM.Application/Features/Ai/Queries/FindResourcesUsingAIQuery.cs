using MediatR;
using PRM.Application.Features.Ai.Dtos;

namespace PRM.Application.Features.Ai.Queries;

public sealed record FindResourcesUsingAIQuery(long ProjectId, string Requirement) : IRequest<SkillMatchResultDto>;
