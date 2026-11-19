namespace UOM.Web.Middleware;

public sealed class TrailingSlashMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && path is { Length: > 1 }
            && path.EndsWith('/'))
        {
            var target = path.TrimEnd('/') + context.Request.QueryString;
            context.Response.Redirect(target, permanent: true);
            return;
        }

        await next(context);
    }
}
