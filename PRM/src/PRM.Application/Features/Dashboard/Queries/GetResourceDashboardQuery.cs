using MediatR;
using PRM.Application.Features.Dashboard.Dtos;

namespace PRM.Application.Features.Dashboard.Queries;

public sealed record GetResourceDashboardQuery : IRequest<ResourceDashboardDto>;
