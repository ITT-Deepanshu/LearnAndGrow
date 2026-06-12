using PRM.Domain.Entities;

using PRM.Application.Common;

namespace PRM.Application.Common;

internal static class UserDisplayHelper
{
    internal static string GetDisplayName(User user) =>
        user.ResourceProfile?.FullName ?? user.Username;
}
