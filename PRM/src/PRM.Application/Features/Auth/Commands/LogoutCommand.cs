using MediatR;

namespace PRM.Application.Features.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
