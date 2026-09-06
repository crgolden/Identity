namespace Identity.CAPTCHA;

#pragma warning disable S101
public sealed class ReCAPTCHAOptions
#pragma warning restore S101
{
    internal const decimal DefaultScoreThreshold = 0.5m;

    public string? SiteKey { get; set; }

    public string? SecretKey { get; set; }

    public decimal ScoreThreshold { get; set; } = DefaultScoreThreshold;

    public Uri? VerifyEndpoint { get; set; }
}
