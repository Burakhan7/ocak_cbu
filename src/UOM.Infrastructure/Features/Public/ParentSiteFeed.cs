using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UOM.Application.Features.Public;

namespace UOM.Infrastructure.Features.Public;

public sealed class ParentSiteFeed(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<ParentSiteOptions> options,
    ILogger<ParentSiteFeed> logger) : IParentSiteFeed
{
    public const string HttpClientName = "ParentSite";
    public const string CacheKey = "parent-site-feed:v1";

    private static readonly string[] RssPaths = ["/feed", "/rss"];

    public async Task<ParentSiteSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var current = options.Value;
        if (!current.Enabled)
        {
            return ParentSiteSnapshot.Empty;
        }

        var snapshot = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            var loaded = await FetchAsync(current, cancellationToken);
            var minutes = Math.Clamp(current.CacheMinutes <= 0 ? 30 : current.CacheMinutes, 5, 180);
            entry.AbsoluteExpirationRelativeToNow = loaded.Items.Count > 0
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.FromMinutes(5);
            return loaded;
        });

        return snapshot ?? ParentSiteSnapshot.Empty;
    }

    private async Task<ParentSiteSnapshot> FetchAsync(ParentSiteOptions current, CancellationToken cancellationToken)
    {
        var origins = (current.Origins is { Length: > 0 } ? current.Origins : ParentSiteOptions.DefaultOrigins)
            .Where(ParentSiteText.IsHttp)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        if (origins.Length == 0)
        {
            return ParentSiteSnapshot.Empty;
        }

        var probes = await Task.WhenAll(origins.Select(origin => ProbeAsync(origin, cancellationToken)));
        var snapshot = ParentSiteFeedSelector.Choose(probes);
        if (snapshot.Items.Count == 0)
        {
            logger.LogWarning("Ana site haberleri alınamadı. Yerel içerik gösterilecek.");
        }
        else
        {
            logger.LogInformation("Ana site kaynağı kullanıldı: {Source}", snapshot.SourceUrl);
        }

        return snapshot;
    }

    private async Task<OriginProbe> ProbeAsync(string origin, CancellationToken cancellationToken)
    {
        var root = origin.TrimEnd('/');
        try
        {
            foreach (var path in RssPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var url = root + path;
                var body = await GetStringAsync(url, HostOf(root), cancellationToken);
                var items = ParentSiteListingParser.ParseFeedXml(body, url);
                if (items.Count > 0)
                {
                    return new OriginProbe(root, items, url, [], null);
                }
            }

            var listingUrl = root + "/blog";
            var listing = await GetStringAsync(listingUrl, HostOf(root), cancellationToken);
            var htmlItems = ParentSiteListingParser.ParseListingHtml(listing, listingUrl);
            return new OriginProbe(root, [], null, htmlItems, htmlItems.Count > 0 ? listingUrl : null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ana site yanıt vermedi: {Origin}", root);
            return new OriginProbe(root, [], null, [], null);
        }
    }

    private async Task<string?> GetStringAsync(string url, string expectedHost, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.RequestMessage?.RequestUri is { } final
            && !final.IdnHost.Equals(expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Ana site isteği beklenmeyen konağa yönlendi: {Host}", final.IdnHost);
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0 || bytes.Length > 700_000)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(bytes);
        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }

    private static string HostOf(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) ? uri.IdnHost : origin;
}
