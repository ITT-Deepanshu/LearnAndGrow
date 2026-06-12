namespace PRM.ConsoleClient.Services;

public sealed class SessionContext
{
    public string? AccessToken { get; set; }
    public string? Role { get; set; }
    public string? FullName { get; set; }
    public long? UserId { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public long? ResourceProfileId { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public void SetLogin(LoginData login)
    {
        AccessToken = login.AccessToken;
        Role = login.Role;
        FullName = login.FullName;
        RequiresPasswordChange = login.RequiresPasswordChange;
        UserId = login.UserId;
        ResourceProfileId = login.ResourceProfileId;
    }

    public void ApplyMe(MeData me)
    {
        UserId = me.Id;
        FullName = me.FullName;
        Role = me.Role;
        RequiresPasswordChange = me.RequiresPasswordChange;
        ResourceProfileId = me.ResourceProfileId;
    }

    public void Clear()
    {
        AccessToken = null;
        Role = null;
        FullName = null;
        UserId = null;
        RequiresPasswordChange = false;
        ResourceProfileId = null;
    }
}

public sealed record LoginData(
    string AccessToken,
    bool RequiresPasswordChange,
    string Role,
    string FullName,
    long? UserId,
    long? ResourceProfileId = null);

public sealed record MeData(
    long Id,
    string FullName,
    string Role,
    bool RequiresPasswordChange,
    long? ResourceProfileId);
