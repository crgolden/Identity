namespace Identity;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

#pragma warning disable S101
public sealed class ReCAPTCHAService : ICAPTCHAService
#pragma warning restore S101
{
    private readonly HttpClient _httpClient;
    private readonly ReCAPTCHAOptions _options;
    private readonly ILogger<ReCAPTCHAService> _logger;

    public ReCAPTCHAService(HttpClient httpClient, IOptions<ReCAPTCHAOptions> options, ILogger<ReCAPTCHAService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string? SiteKey => _options.SiteKey;

    public decimal ScoreThreshold => _options.ScoreThreshold;

    public bool IsExempt(string? email) =>
        !IsNullOrWhiteSpace(email) &&
        (string.Equals(email, _options.AdminEmail, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(email, _options.TestEmail, StringComparison.OrdinalIgnoreCase));

    public async Task<decimal> VerifyAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (IsNullOrWhiteSpace(token) || IsNullOrWhiteSpace(_options.SecretKey))
        {
            return 0m;
        }

        var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("secret", _options.SecretKey),
            new KeyValuePair<string, string>("response", token)
        ]);

        var response = await _httpClient.PostAsync(_options.VerifyEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("reCAPTCHA siteverify returned {StatusCode}.", response.StatusCode);
            return 0m;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken);
        if (result is null || !result.Success)
        {
            _logger.LogWarning("reCAPTCHA verification failed.");
            return 0m;
        }

        _logger.LogDebug("reCAPTCHA score: {Score}.", result.Score);
        return result.Score;
    }
}

internal sealed record RecaptchaResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("score")] decimal Score);
