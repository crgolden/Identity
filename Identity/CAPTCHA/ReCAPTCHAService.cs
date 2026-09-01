namespace Identity.CAPTCHA;

using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

#pragma warning disable S101
public sealed class ReCAPTCHAService : ICAPTCHAService
#pragma warning restore S101
{
    internal const string SyntheticMarkerHeaderName = "X-Synthetic-Marker";

    private readonly HttpClient _httpClient;
    private readonly ReCAPTCHAOptions _options;

    public ReCAPTCHAService(HttpClient httpClient, IOptions<ReCAPTCHAOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string? SiteKey => _options.SiteKey;

    public async Task<CAPTCHAVerdict> VerifyAsync(
        string action,
        string? email,
        string? token,
        string? syntheticMarker,
        CancellationToken cancellationToken = default)
    {
        var score = await GetScoreAsync(token, cancellationToken);
        if (IsMarkedSynthetic(email, syntheticMarker))
        {
            Telemetry.Metrics.SyntheticCaptchaObserved(action, score);
            return new CAPTCHAVerdict(Passed: true, score, MonitorOnly: true);
        }

        return new CAPTCHAVerdict(score >= _options.ScoreThreshold, score, MonitorOnly: false);
    }

    private async Task<decimal> GetScoreAsync(string? token, CancellationToken cancellationToken)
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
            return 0m;
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken);
        if (result is null || !result.Success)
        {
            return 0m;
        }

        return result.Score;
    }

    private bool IsMarkedSynthetic(string? email, string? syntheticMarker)
    {
        if (IsNullOrWhiteSpace(_options.SyntheticMarkerSecret)
            || IsNullOrWhiteSpace(syntheticMarker)
            || IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var configuredMarkerBytes = UTF8.GetBytes(_options.SyntheticMarkerSecret);
        var presentedMarkerBytes = UTF8.GetBytes(syntheticMarker);
        return CryptographicOperations.FixedTimeEquals(presentedMarkerBytes, configuredMarkerBytes)
            && _options.TestEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed record RecaptchaResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("score")] decimal Score);
