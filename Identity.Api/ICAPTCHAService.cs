namespace Identity;

#pragma warning disable S101
public interface ICAPTCHAService
#pragma warning restore S101
{
    Task<decimal> VerifyAsync(string? token, CancellationToken cancellationToken = default);
}
