namespace Identity.CAPTCHA;

public interface ICAPTCHAService
{
    string? SiteKey { get; }

    Uri? ScriptEndpoint { get; }

    Task<CAPTCHAVerdict> VerifyAsync(string? token, CancellationToken cancellationToken = default);
}
