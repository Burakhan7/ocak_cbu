using Microsoft.Extensions.Configuration;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        var localRoot = configuration["Storage:LocalRoot"] ?? "wwwroot/uploads";
        _root = Path.IsPathRooted(localRoot)
            ? localRoot
            : Path.Combine(Directory.GetCurrentDirectory(), localRoot);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetAbsolutePath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
        return relativePath.Replace('\\', '/');
    }

    public string GetAbsolutePath(string relativePath) =>
        Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public bool Exists(string relativePath) => File.Exists(GetAbsolutePath(relativePath));
}