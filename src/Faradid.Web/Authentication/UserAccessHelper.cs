using System;

namespace Faradid.Web.Authentication;

// Forwarding alias for backward compatibility within Web layer
public static class UserAccessHelper
{
    public static bool IsAdmin(System.Security.Claims.ClaimsPrincipal? user) 
        => Faradid.Core.Security.UserAccessHelper.IsAdmin(user);

    public static bool CanAccessProject(System.Security.Claims.ClaimsPrincipal? user, Faradid.Core.Models.ProjectProfile project) 
        => Faradid.Core.Security.UserAccessHelper.CanAccessProject(user, project);

    public static System.Collections.Generic.IReadOnlyList<Faradid.Core.Models.ProjectProfile> FilterProjects(
        System.Security.Claims.ClaimsPrincipal? user, 
        System.Collections.Generic.IEnumerable<Faradid.Core.Models.ProjectProfile> projects) 
        => Faradid.Core.Security.UserAccessHelper.FilterProjects(user, projects);
}
