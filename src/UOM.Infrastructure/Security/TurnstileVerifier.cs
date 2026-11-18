using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UOM.Application.Abstractions;

namespace UOM.Infrastructure.Security;

public sealed class TurnstileVerifier : ITurnstileVerifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TurnstileVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue("Turnstile:Enabled", false);
        if (!enabled)
        {
            return true;
        }

        var secret = _configuration["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(TurnstileVerifier));
            var response = await client.PostAsJsonAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new { secret, response = token },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken);
            return json?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Turnstile doğrulaması başarısız");
            return false;
        }
    }

    private sealed record TurnstileResponse(bool Success);
}