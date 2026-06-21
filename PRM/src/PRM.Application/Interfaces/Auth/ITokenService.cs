using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user, IReadOnlyList<string> permissions);
}
