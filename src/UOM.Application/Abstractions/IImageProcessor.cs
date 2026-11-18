namespace UOM.Application.Abstractions;

public sealed record ProcessedImageVariants(
    string OriginalRelativePath,
    int? Width,
    int? Height,
    IReadOnlyDictionary<string, string> WebpVariantPaths);

public interface IImageProcessor
{
    Task<ProcessedImageVariants> ProcessUploadAsync(
        Stream input,
        string storageFolder,
        string baseFileName,
        CancellationToken cancellationToken = default);
}