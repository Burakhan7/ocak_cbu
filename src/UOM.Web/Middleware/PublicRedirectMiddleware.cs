using UOM.Application.Features.Public;

namespace UOM.Web.Middleware;

public sealed class PublicRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IPublicRedirectService redirects)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value ?? "/";
            if (!LooksLikeStaticFile(path))
            {
                var match = await redirects.FindAsync(path, context.RequestAborted);
                if (match is not null)
                {
                    await redirects.RegisterHitAsync(path, context.RequestAborted);
                    if (match.StatusCode == 301)
                    {
                        context.Response.Redirect(match.ToPath, permanent: true);
                    }
                    else if (match.StatusCode == 302)
                    {
                        context.Response.Redirect(match.ToPath, permanent: false);
                    }
                    else
                    {
                        context.Response.StatusCode = match.StatusCode;
                        context.Response.Headers.Location = match.ToPath;
                    }

                    return;
                }
            }
        }

        await next(context);
    }

    private static bool LooksLikeStaticFile(string path)
    {
        var dot = path.LastIndexOf('.');
        if (dot < 0 || dot < path.LastIndexOf('/'))
        {
            return false;
        }

        var extension = path[dot..];
        return extension is ".css" or ".js" or ".map" or ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".svg" or ".ico" or ".woff" or ".woff2";
    }
}
