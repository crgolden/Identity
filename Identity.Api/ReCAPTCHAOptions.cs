namespace Identity;

/// <summary>Options for Google reCAPTCHA v3 verification.</summary>
#pragma warning disable S101
public sealed class ReCAPTCHAOptions
#pragma warning restore S101
{
    public string? SiteKey { get; set; }

    public string? SecretKey { get; set; }

    public decimal ScoreThreshold { get; set; } = 0.5m;

    public Uri? VerifyEndpoint { get; set; }

    public string? SmokeTestEmail { get; set; }
}
