using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class User : AuditableEntity
{
    private User() { }

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool ForcePasswordChange { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public Employee? EmployeeProfile { get; private set; }

    public static User Create(
        string username,
        string email,
        string fullName,
        string passwordHash,
        UserRole role,
        long createdBy,
        DateTime utcNow,
        bool forcePasswordChange = true)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ValidationException("Username is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");

        var user = new User
        {
            Username = username.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            ForcePasswordChange = forcePasswordChange,
            IsActive = true
        };
        user.SetCreated(createdBy, utcNow);
        return user;
    }

    public void ChangePassword(string newPasswordHash, long actorId, DateTime utcNow)
    {
        PasswordHash = newPasswordHash;
        ForcePasswordChange = false;
        SetModified(actorId, utcNow);
    }

    public void RequirePasswordChange(long actorId, DateTime utcNow)
    {
        ForcePasswordChange = true;
        SetModified(actorId, utcNow);
    }

    public void Deactivate(long actorId, DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        SetModified(actorId, utcNow);
    }

    public void Reactivate(long actorId, DateTime utcNow)
    {
        if (IsActive) return;
        IsActive = true;
        SetModified(actorId, utcNow);
    }

    public void RecordLogin(DateTime utcNow)
    {
        LastLoginAt = utcNow;
    }
}
