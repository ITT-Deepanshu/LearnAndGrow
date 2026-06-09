using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
    (long userId, string username, string role, bool forcePasswordChange)? ValidateAccessToken(string token);
}
