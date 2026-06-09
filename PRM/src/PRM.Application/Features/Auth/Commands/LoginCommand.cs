using MediatR;
using PRM.Application.Features.Auth.Dtos;

namespace PRM.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResultDto>;
