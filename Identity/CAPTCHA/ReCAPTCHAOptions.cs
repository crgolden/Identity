namespace Identity.CAPTCHA;

public sealed class ReCAPTCHAOptions
{
    internal const decimal DefaultScoreThreshold = 0.5m;

    public string? SiteKey { get; set; }

    public string? SecretKey { get; set; }

    public decimal ScoreThreshold { get; set; } = DefaultScoreThreshold;

    public Uri? VerifyEndpoint { get; set; }

    public Uri? ScriptEndpoint { get; set; }
}
