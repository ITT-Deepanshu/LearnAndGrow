using MediatR;
using PRM.Application.Features.Auth.Dtos;

namespace PRM.Application.Features.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultDto>;
