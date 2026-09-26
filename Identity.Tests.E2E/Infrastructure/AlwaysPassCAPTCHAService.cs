namespace Identity.Tests.E2E.Infrastructure;

using Identity.CAPTCHA;
using Microsoft.Extensions.Options;

internal sealed class AlwaysPassCAPTCHAService(IOptions<ReCAPTCHAOptions> options) : ICAPTCHAService
{
    public string? SiteKey => null;

    public Uri? ScriptEndpoint => options.Value.ScriptEndpoint;

    public Task<CAPTCHAVerdict> VerifyAsync(string? token, CancellationToken cancellationToken = default)
        => Task.FromResult(new CAPTCHAVerdict(Passed: true, Score: 1.0m));
}
