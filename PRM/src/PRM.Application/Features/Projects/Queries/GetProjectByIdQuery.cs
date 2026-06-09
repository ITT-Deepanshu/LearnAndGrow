using MediatR;
using PRM.Application.Features.Projects.Dtos;

namespace PRM.Application.Features.Projects.Queries;

public sealed record GetProjectByIdQuery(long ProjectId) : IRequest<ProjectDetailDto>;
