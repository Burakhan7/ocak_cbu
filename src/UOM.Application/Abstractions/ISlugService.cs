namespace UOM.Application.Abstractions;

public interface ISlugService
{
    Task<string> GenerateSlugAsync(string title, Func<string, Task<bool>> slugExistsAsync, CancellationToken cancellationToken = default);
}