using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PRM.Api.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string AnyPrefix = "Any:";
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            return _fallback.GetPolicyAsync(policyName);

        if (policyName.StartsWith(AnyPrefix, StringComparison.Ordinal))
        {
            var permissions = policyName[AnyPrefix.Length..]
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permissions, requireAll: false))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        var singlePolicy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement([policyName], requireAll: true))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(singlePolicy);
    }
}
