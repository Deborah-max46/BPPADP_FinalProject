using System.Security.Claims;
using ConsumersVoiceSystemPrototype.Models;

namespace ConsumersVoiceSystemPrototype.Services;

/// <summary>
/// Maps Identity roles to the primary dashboard URL. Order matches login redirect priority.
/// </summary>
public static class UserDashboardRedirect
{
    private static readonly (string Role, string Path)[] Ordered =
    {
        (RoleNames.Consumer, "~/Dashboard/Consumer"),
        (RoleNames.Advocate, "~/Dashboard/Advocate"),
        (RoleNames.Business, "~/Dashboard/Business"),
        (RoleNames.Admin, "~/Dashboard/Admin"),
    };

    /// <summary>Landing page for anonymous users; role dashboard when signed in.</summary>
    public static string GetLayoutHomePath(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return "~/Index";
        return GetDashboardPathFromPrincipal(user);
    }

    public static string GetDashboardPathFromPrincipal(ClaimsPrincipal user)
    {
        foreach (var (role, path) in Ordered)
        {
            if (user.IsInRole(role))
                return path;
        }

        return "~/Dashboard/Index";
    }

    public static string GetDashboardPathFromRoleNames(IEnumerable<string> roles)
    {
        var set = roles as IReadOnlyCollection<string> ?? roles.ToList();
        foreach (var (role, path) in Ordered)
        {
            if (set.Contains(role))
                return path;
        }

        return "~/Dashboard/Index";
    }
}
