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

        result.Width.Should().Be(1200);
        result.Height.Should().Be(800);
        await using var original = File.OpenRead(storage.GetAbsolutePath(result.OriginalRelativePath));
        var saved = await Image.IdentifyAsync(original);
        saved.Width.Should().Be(1200);
        saved.Height.Should().Be(800);
    }

    [Theory]
    [InlineData(3200, 1800, 1600, 900)]
    [InlineData(1800, 3200, 900, 1600)]
    [InlineData(1600, 900, 1600, 900)]
    [InlineData(800, 500, 800, 500)]
    public async Task ProcessUpload_caps_long_edge_without_cropping(int width, int height, int expectedWidth, int expectedHeight)
    {
        var root = Path.Combine(Path.GetTempPath(), "uom-img-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:LocalRoot"] = root })
            .Build();

        var storage = new LocalFileStorage(configuration);
        var processor = new ImageSharpImageProcessor(storage);

        await using var png = new MemoryStream();
        using (var image = new Image<Rgba32>(width, height))
        {
            await image.SaveAsPngAsync(png);
        }

        png.Position = 0;
        var result = await processor.ProcessUploadAsync(png, "images/test", "sample", CancellationToken.None);

        result.Width.Should().Be(expectedWidth);
        result.Height.Should().Be(expectedHeight);
        Math.Max(result.Width!.Value, result.Height!.Value).Should().BeLessThanOrEqualTo(ImageSharpImageProcessor.MaxLongEdgePixels);

        await using var original = File.OpenRead(storage.GetAbsolutePath(result.OriginalRelativePath));
        var saved = await Image.IdentifyAsync(original);
        saved.Width.Should().Be(expectedWidth);
        saved.Height.Should().Be(expectedHeight);

        result.WebpVariantPaths.Should().ContainKeys("thumb", "medium", "large");
        foreach (var path in result.WebpVariantPaths.Values.Append(result.OriginalRelativePath))
        {
            await using var variant = File.OpenRead(storage.GetAbsolutePath(path));
            var info = await Image.IdentifyAsync(variant);
            Math.Max(info.Width, info.Height).Should().BeLessThanOrEqualTo(ImageSharpImageProcessor.MaxLongEdgePixels);
            if (expectedWidth > 0 && expectedHeight > 0)
            {
                var sourceRatio = expectedWidth / (double)expectedHeight;
                var variantRatio = info.Width / (double)info.Height;
                variantRatio.Should().BeApproximately(sourceRatio, 0.02);
            }
        }
    }
}