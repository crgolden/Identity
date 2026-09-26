namespace Identity.CAPTCHA;

using System.Text.Json.Serialization;

internal sealed record RecaptchaResponse(
    [property: JsonPropertyName(ReCAPTCHAService.SiteverifySuccessFieldName)] bool Success,
    [property: JsonPropertyName(ReCAPTCHAService.SiteverifyScoreFieldName)] decimal Score);
