using MediatR;
using PRM.Application.Features.Ai.Dtos;

namespace PRM.Application.Features.Ai.Queries;

public sealed record GetProjectRiskSummaryQuery(long ProjectId) : IRequest<RiskSummaryDto>;
