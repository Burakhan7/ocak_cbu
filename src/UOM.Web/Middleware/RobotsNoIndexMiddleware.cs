namespace UOM.Web.Middleware;

public sealed class RobotsNoIndexMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Robots-Tag"] = "noindex";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
