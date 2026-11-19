using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Features.Admin;

internal static class SlugEdits
{
    public static async Task<string> ResolveAsync(
        ISlugService slugService,
        string currentSlug,
        string? requestedSlug,
        Func<string, Task<bool>> takenByOther,
        CancellationToken cancellationToken)
    {
        var source = string.IsNullOrWhiteSpace(requestedSlug) ? currentSlug : requestedSlug.Trim();
        return await slugService.GenerateSlugAsync(
            source,
            async candidate =>
            {
                if (string.Equals(candidate, currentSlug, StringComparison.Ordinal))
                {
                    return false;
                }

                return await takenByOther(candidate);
            },
            cancellationToken);
    }
}

internal static class PublishedSlug
{
    public static async Task ApplyAsync(
        ISlugService slugService,
        IRedirectRecorder? redirects,
        string currentSlug,
        string? requestedSlug,
        UOM.Domain.Enums.PublishStatus status,
        Func<string, string> publicPath,
        Func<string, Task<bool>> takenByOther,
        Action<string> assign,
        CancellationToken cancellationToken)
    {
        var resolved = await SlugEdits.ResolveAsync(
            slugService,
            currentSlug,
            requestedSlug,
            takenByOther,
            cancellationToken);

        if (!string.Equals(currentSlug, resolved, StringComparison.Ordinal)
            && status == UOM.Domain.Enums.PublishStatus.Published
            && redirects is not null)
        {
            await redirects.StagePermanentAsync(publicPath(currentSlug), publicPath(resolved), cancellationToken);
        }

        assign(resolved);
    }
}
