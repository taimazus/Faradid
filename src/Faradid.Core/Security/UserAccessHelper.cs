using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Faradid.Core.Models;

namespace Faradid.Core.Security;

public static class UserAccessHelper
{
    public static bool IsAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;

        // Check Roles (Admin, SuperAdmin, SeoSpecialist, etc.)
        if (user.IsInRole("Admin") ||
            user.IsInRole("SuperAdmin") ||
            user.IsInRole("superadmin") ||
            user.IsInRole("Administrator") ||
            user.IsInRole("SeoSpecialist") ||
            user.IsInRole("IrresAdmin") ||
            user.Claims.Any(c => c.Type == ClaimTypes.Role &&
                (c.Value.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                 c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                 c.Value.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                 c.Value.Contains("ابر مدیر", StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(email) &&
            (email.Equals("admin@irres.ir", StringComparison.OrdinalIgnoreCase) ||
             email.StartsWith("admin@", StringComparison.OrdinalIgnoreCase) ||
             email.StartsWith("superadmin@", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var username = user.FindFirst("Username")?.Value ?? string.Empty;
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            username.Equals("superadmin", StringComparison.OrdinalIgnoreCase) ||
            username.Equals("sahand", StringComparison.OrdinalIgnoreCase) ||
            username.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var name = user.Identity.Name ?? string.Empty;
        if (name.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("superadmin", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ابر مدیر", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("مدیر کل", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("مدیر سیستم", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool CanAccessProject(ClaimsPrincipal? user, ProjectProfile project)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;
        if (IsAdmin(user)) return true;

        var username = user.FindFirst("Username")?.Value ?? user.Identity.Name ?? string.Empty;
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(project.OwnerUsername) &&
            project.OwnerUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(project.OwnerEmail) &&
            project.OwnerEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Domain prefix match (e.g. username 'yonarak' matching 'yonarak.ir')
        var domainRoot = project.Domain.Split('.')[0];
        if (!string.IsNullOrWhiteSpace(domainRoot) &&
            (username.Contains(domainRoot, StringComparison.OrdinalIgnoreCase) ||
             domainRoot.Contains(username, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public static IReadOnlyList<ProjectProfile> FilterProjects(ClaimsPrincipal? user, IEnumerable<ProjectProfile> projects)
    {
        if (user?.Identity?.IsAuthenticated != true) return Array.Empty<ProjectProfile>();
        if (IsAdmin(user)) return projects.ToList();

        return projects.Where(p => CanAccessProject(user, p)).ToList();
    }
}
