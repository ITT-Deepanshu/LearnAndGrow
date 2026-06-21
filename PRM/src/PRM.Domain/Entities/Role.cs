namespace PRM.Domain.Entities;

public class Role
{
    private readonly List<RolePermission> _permissions = [];

    private Role() { }

    public long Id { get; private set; }
    public string RoleName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Role Create(long id, string roleName, string description)
    {
        return new Role
        {
            Id = id,
            RoleName = roleName.Trim(),
            Description = description.Trim()
        };
    }
}
