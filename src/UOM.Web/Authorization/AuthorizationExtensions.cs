using UOM.Application.Authorization;
using UOM.Domain.Constants;

namespace UOM.Web.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddUomAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.CanPublish, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));

            options.AddPolicy(PolicyNames.CanManageUsers, p =>
                p.RequireRole(RoleNames.SuperAdmin));

            options.AddPolicy(PolicyNames.CanManageSiteSettings, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));

            options.AddPolicy(PolicyNames.CanViewContactMessages, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));

            options.AddPolicy(PolicyNames.CanViewAuditLog, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));

            options.AddPolicy(PolicyNames.CanManageParentLayer, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin));

            options.AddPolicy(PolicyNames.CanWriteProvinceContent, p =>
                p.RequireRole(RoleNames.SuperAdmin, RoleNames.ProvinceAdmin, RoleNames.Editor));

            options.AddPolicy(PolicyNames.CanWriteCampusContent, p =>
                p.RequireRole(
                    RoleNames.SuperAdmin,
                    RoleNames.ProvinceAdmin,
                    RoleNames.Editor,
                    RoleNames.CampusEditor));
        });

        return services;
    }
}