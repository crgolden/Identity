namespace Identity;

#pragma warning disable S101
public interface ICAPTCHAService
#pragma warning restore S101
{
    string? SiteKey { get; }

    decimal ScoreThreshold { get; }

    Task<decimal> VerifyAsync(string? token, CancellationToken cancellationToken = default);
}
