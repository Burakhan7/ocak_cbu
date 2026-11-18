namespace UOM.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default);

    string GetAbsolutePath(string relativePath);

    bool Exists(string relativePath);
}