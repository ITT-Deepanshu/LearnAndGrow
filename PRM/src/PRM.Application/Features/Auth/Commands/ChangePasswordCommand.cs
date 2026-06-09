using MediatR;

namespace PRM.Application.Features.Auth.Commands;

public sealed record ChangePasswordCommand(
    string? CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest;
