using MediatR;
using PRM.Application.Features.Users.Dtos;

namespace PRM.Application.Features.Users.Queries;

public sealed record GetUserByIdQuery(long UserId) : IRequest<UserDto>;
