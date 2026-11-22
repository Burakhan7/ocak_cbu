using UOM.Web.Security;

namespace UOM.Web.Middleware;

public sealed class UploadGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
        {
            if (!UploadContentPolicy.IsServable(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.OnStarting(() =>
            {
                context.Response.Headers.XContentTypeOptions = "nosniff";
                context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
