using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Authorization;
using UOM.Domain.Constants;
namespace UOM.Application.Tests.Authorization;

public class PublishAuthorizationTests
{
    [Theory]
    [InlineData(RoleNames.Editor, false)]
    [InlineData(RoleNames.CampusEditor, false)]
    [InlineData(RoleNames.ProvinceAdmin, true)]
    [InlineData(RoleNames.SuperAdmin, true)]
    public async Task CanPublish_policy_matches_role_matrix(string role, bool expected)
    {
        var auth = BuildAuthorizationService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
        var result = await auth.AuthorizeAsync(principal, PolicyNames.CanPublish);
        result.Succeeded.Should().Be(expected);
    }

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.CanPublish, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));
        });
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }
}