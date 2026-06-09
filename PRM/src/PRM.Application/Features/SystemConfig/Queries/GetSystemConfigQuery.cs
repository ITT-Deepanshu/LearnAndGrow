using MediatR;
using PRM.Application.Features.SystemConfig.Dtos;

namespace PRM.Application.Features.SystemConfig.Queries;

public sealed record GetSystemConfigQuery : IRequest<SystemConfigDto>;
