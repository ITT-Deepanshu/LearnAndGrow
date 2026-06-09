namespace PRM.ConsoleClient.Services;

public sealed class SessionContext
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Role { get; set; }
    public string? FullName { get; set; }
    public long? UserId { get; set; }
    public bool ForcePasswordChange { get; set; }
    public long? EmployeeId { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public void SetLogin(LoginData login)
    {
        AccessToken = login.AccessToken;
        RefreshToken = login.RefreshToken;
        Role = login.Role;
        FullName = login.FullName;
        ForcePasswordChange = login.ForcePasswordChange;
        UserId = login.UserId;
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        Role = null;
        FullName = null;
        UserId = null;
        ForcePasswordChange = false;
        EmployeeId = null;
    }
}

public sealed record LoginData(
    string AccessToken,
    string RefreshToken,
    bool ForcePasswordChange,
    string Role,
    string FullName,
    long? UserId);
