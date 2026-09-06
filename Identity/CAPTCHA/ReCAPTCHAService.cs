namespace Identity.CAPTCHA;

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

    public ReCAPTCHAService(HttpClient httpClient, IOptions<ReCAPTCHAOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string? SiteKey => _options.SiteKey;

    public async Task<CAPTCHAVerdict> VerifyAsync(string? token, CancellationToken cancellationToken = default)
    {
        var score = await GetScoreAsync(token, cancellationToken);
        return new CAPTCHAVerdict(score >= _options.ScoreThreshold, score);
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
}

internal sealed record RecaptchaResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("score")] decimal Score);
