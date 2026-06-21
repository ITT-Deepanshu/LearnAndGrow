using Microsoft.AspNetCore.Authorization;

namespace PRM.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute(permission);

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        Policy = AnyPrefix + string.Join('|', permissions.OrderBy(p => p, StringComparer.Ordinal));
    }

    private const string AnyPrefix = "Any:";
}
