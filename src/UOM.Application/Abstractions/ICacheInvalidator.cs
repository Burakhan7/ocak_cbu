namespace UOM.Application.Abstractions;

public interface ICacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken = default, params string[] tags);
}
