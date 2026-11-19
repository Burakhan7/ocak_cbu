using FluentAssertions;
using UOM.Infrastructure.Features.Public;

namespace UOM.Application.Tests.Features.Public;

public class MediaUrlHelperTests
{
    [Theory]
    [InlineData("images/2026/11/abc-medium.webp", "/uploads/images/2026/11/abc-medium.webp")]
    [InlineData("/images/2026/11/abc-medium.webp", "/uploads/images/2026/11/abc-medium.webp")]
    [InlineData("files/2026/11/doc.pdf", "/uploads/files/2026/11/doc.pdf")]
    [InlineData("uploads/images/already.webp", "/uploads/images/already.webp")]
    public void ToPublicUrl_maps_storage_path_under_wwwroot_uploads(string storagePath, string expected)
    {
        MediaUrlHelper.ToPublicUrl(storagePath).Should().Be(expected);
    }

    [Fact]
    public void ResolveUrl_prefers_variant_from_json()
    {
        const string variants = """{"thumb":"images/x-thumb.webp","medium":"images/x-medium.webp"}""";
        MediaUrlHelper.ResolveUrl("images/x.webp", variants, "medium")
            .Should().Be("/uploads/images/x-medium.webp");
    }
}