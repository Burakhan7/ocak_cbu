using System.Text;
using UOM.Web.Security;

namespace UOM.Web.Middleware;

public sealed class CspNonceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = CspNonce.Create();
        context.Items[CspNonce.HttpContextItemKey] = nonce;

        if (!MayContainHtml(context.Request.Path))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        var contentType = context.Response.ContentType ?? string.Empty;
        if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            if (buffer.Length > 0)
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
            }

            return;
        }

        buffer.Position = 0;
        using var reader = new StreamReader(buffer, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();
        html = html.Replace(CspNonce.Placeholder, nonce, StringComparison.Ordinal);
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentLength = bytes.Length;
        context.Response.Headers.ContentSecurityPolicy = CspPolicy.Build(nonce, context.Request.IsHttps);
        await originalBody.WriteAsync(bytes);
    }

    private static bool MayContainHtml(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.Contains("/indir", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(value);
        return string.IsNullOrEmpty(extension)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
    }
}
