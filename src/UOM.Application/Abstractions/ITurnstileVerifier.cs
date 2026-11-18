namespace UOM.Application.Abstractions;

public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, CancellationToken cancellationToken = default);
}