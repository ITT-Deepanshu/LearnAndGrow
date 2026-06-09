using MediatR;
using PRM.Application.Features.Auth.Dtos;

namespace PRM.Application.Features.Auth.Queries;

public sealed record GetMeQuery : IRequest<MeDto>;
