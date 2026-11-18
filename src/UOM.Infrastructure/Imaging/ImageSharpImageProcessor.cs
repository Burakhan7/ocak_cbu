using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Imaging;

public sealed class ImageSharpImageProcessor(IFileStorage fileStorage) : IImageProcessor
{
    private static readonly int[] VariantWidths = [400, 960, 1920];
    private static readonly string[] VariantNames = ["thumb", "medium", "large"];

    public async Task<ProcessedImageVariants> ProcessUploadAsync(
        Stream input,
        string storageFolder,
        string baseFileName,
        CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        var originalRelative = $"{storageFolder}/{baseFileName}.webp";
        var variants = new Dictionary<string, string>();

        await using (var originalMs = new MemoryStream())
        {
            await image.SaveAsWebpAsync(originalMs, new WebpEncoder { Quality = 90 }, cancellationToken);
            originalMs.Position = 0;
            await fileStorage.SaveAsync(originalMs, originalRelative, cancellationToken);
        }

        for (var i = 0; i < VariantWidths.Length; i++)
        {
            var name = VariantNames[i];
            var maxWidth = VariantWidths[i];
            using var clone = image.Clone(ctx =>
            {
                if (image.Width > maxWidth)
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, 0),
                    });
                }
            });

            var path = $"{storageFolder}/{baseFileName}-{name}.webp";
            await using var ms = new MemoryStream();
            await clone.SaveAsWebpAsync(ms, new WebpEncoder { Quality = 85 }, cancellationToken);
            ms.Position = 0;
            await fileStorage.SaveAsync(ms, path, cancellationToken);
            variants[name] = path;
        }

        return new ProcessedImageVariants(
            originalRelative,
            image.Width,
            image.Height,
            variants);
    }
}