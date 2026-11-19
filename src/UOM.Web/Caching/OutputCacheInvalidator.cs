using Microsoft.AspNetCore.OutputCaching;
using UOM.Application.Abstractions;

namespace UOM.Web.Caching;

public sealed class OutputCacheInvalidator(IOutputCacheStore store) : ICacheInvalidator
{
    public async Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] tags)
    {
        foreach (var tag in tags.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            await store.EvictByTagAsync(tag, cancellationToken);
        }
    }
}
