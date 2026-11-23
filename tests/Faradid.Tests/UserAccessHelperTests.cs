using System;
using System.Collections.Generic;
using System.Security.Claims;
using Faradid.Core.Models;
using Faradid.Core.Security;
using Xunit;

namespace Faradid.Tests;

public sealed class UserAccessHelperTests
{
    private readonly List<ProjectProfile> _sampleProjects = new()
    {
        new ProjectProfile
        {
            Id = Guid.NewGuid(),
            Name = "راهکار الکترونیک سهند",
            Domain = "irres.ir",
            OwnerUsername = "irres",
            OwnerEmail = "info@irres.ir"
        },
        new ProjectProfile
        {
            Id = Guid.NewGuid(),
            Name = "یونارک",
            Domain = "yonarak.ir",
            OwnerUsername = "yonarak",
            OwnerEmail = "manager@yonarak.ir"
        },
        new ProjectProfile
        {
            Id = Guid.NewGuid(),
            Name = "آسا زیست",
            Domain = "asazist.com",
            OwnerUsername = "asazist",
            OwnerEmail = "info@asazist.com"
        }
    };

    [Fact]
    public void UnauthenticatedUser_CannotAccessProjects()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        
        Assert.False(UserAccessHelper.IsAdmin(anonymous));
        Assert.False(UserAccessHelper.CanAccessProject(anonymous, _sampleProjects[0]));
        
        var filtered = UserAccessHelper.FilterProjects(anonymous, _sampleProjects);
        Assert.Empty(filtered);
    }

    [Fact]
    public void AdminUser_CanAccessAllProjects()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "مدیر سیستم (سهند)"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Email, "admin@irres.ir")
        };
        var admin = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        Assert.True(UserAccessHelper.IsAdmin(admin));
        Assert.True(UserAccessHelper.CanAccessProject(admin, _sampleProjects[0]));
        Assert.True(UserAccessHelper.CanAccessProject(admin, _sampleProjects[1]));
        Assert.True(UserAccessHelper.CanAccessProject(admin, _sampleProjects[2]));

        var filtered = UserAccessHelper.FilterProjects(admin, _sampleProjects);
        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void ClientUser_OnlyAccessesOwnProject()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "yonarak"),
            new Claim(ClaimTypes.Role, "Client"),
            new Claim(ClaimTypes.Email, "manager@yonarak.ir"),
            new Claim("Username", "yonarak")
        };
        var client = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        Assert.False(UserAccessHelper.IsAdmin(client));
        Assert.True(UserAccessHelper.CanAccessProject(client, _sampleProjects[1])); // yonarak.ir
        Assert.False(UserAccessHelper.CanAccessProject(client, _sampleProjects[0])); // irres.ir
        Assert.False(UserAccessHelper.CanAccessProject(client, _sampleProjects[2])); // asazist.com

        var filtered = UserAccessHelper.FilterProjects(client, _sampleProjects);
        Assert.Single(filtered);
        Assert.Equal("yonarak.ir", filtered[0].Domain);
    }
}
