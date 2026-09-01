namespace Identity.CAPTCHA;

#pragma warning disable S101
public interface ICAPTCHAService
#pragma warning restore S101
{
    string? SiteKey { get; }

    Task<CAPTCHAVerdict> VerifyAsync(
        string action,
        string? email,
        string? token,
        string? syntheticMarker,
        CancellationToken cancellationToken = default);
}
