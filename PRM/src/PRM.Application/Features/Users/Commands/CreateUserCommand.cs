using MediatR;
using PRM.Application.Features.Users.Dtos;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Users.Commands;

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    UserRole Role) : IRequest<UserDto>;
