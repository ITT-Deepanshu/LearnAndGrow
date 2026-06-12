namespace PRM.Domain.Entities;

public class RolePermission
{
    private RolePermission() { }

    public long Id { get; private set; }
    public long RoleId { get; private set; }
    public string Permission { get; private set; } = string.Empty;

    public Role Role { get; private set; } = null!;

    public static RolePermission Create(long id, long roleId, string permission)
    {
        return new RolePermission
        {
            Id = id,
            RoleId = roleId,
            Permission = permission.Trim()
        };
    }
}
