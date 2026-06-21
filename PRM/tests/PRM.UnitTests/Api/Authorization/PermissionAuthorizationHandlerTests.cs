using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using PRM.Api.Authorization;
using PRM.Domain.Constants;

namespace PRM.UnitTests.Api.Authorization;

public class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_SucceedsWhenPermissionClaimPresent()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(PrmClaimTypes.Permission, RolePermissions.AiUse)
        ], "Test"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement([RolePermissions.AiUse])],
            user,
            null);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_FailsWhenPermissionMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(PrmClaimTypes.Permission, RolePermissions.TimesheetsSubmit)
        ], "Test"));

        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement([RolePermissions.AiUse])],
            user,
            null);

        var handler = new PermissionAuthorizationHandler();
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
