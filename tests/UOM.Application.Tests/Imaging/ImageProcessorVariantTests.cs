using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UOM.Infrastructure.Imaging;
using UOM.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace UOM.Application.Tests.Imaging;

public class ImageProcessorVariantTests
{
    [Fact]
    public async Task ProcessUpload_creates_three_webp_variants()
    {
        var root = Path.Combine(Path.GetTempPath(), "uom-img-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:LocalRoot"] = root })
            .Build();

        var storage = new LocalFileStorage(configuration);
        var processor = new ImageSharpImageProcessor(storage);

        await using var png = new MemoryStream();
        using (var image = new Image<Rgba32>(1200, 800))
        {
            await image.SaveAsPngAsync(png);
        }

        png.Position = 0;
        var result = await processor.ProcessUploadAsync(png, "images/test", "sample", CancellationToken.None);

        result.WebpVariantPaths.Should().ContainKeys("thumb", "medium", "large");
        storage.Exists(result.OriginalRelativePath).Should().BeTrue();
        foreach (var path in result.WebpVariantPaths.Values)
        {
            storage.Exists(path).Should().BeTrue();
        }
    }
}