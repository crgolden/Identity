namespace Identity;

#pragma warning disable S101
public interface ICAPTCHAService
#pragma warning restore S101
{
    string? SiteKey { get; }

    decimal ScoreThreshold { get; }

    bool IsExempt(string? email);

    Task<decimal> VerifyAsync(string? token, CancellationToken cancellationToken = default);
}
