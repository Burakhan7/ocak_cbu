using UOM.Application.Features.Public;

namespace UOM.Web.Routing;

/// <summary>
/// Prevents the catch-all static page route from matching fixed application paths.
/// </summary>
public sealed class NonReservedSlugRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var raw) || raw is null)
        {
            return false;
        }

        var slug = raw.ToString();
        return !string.IsNullOrEmpty(slug) && !ReservedPublicSlugs.IsReserved(slug);
    }
}