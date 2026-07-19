namespace Identity.Tests.E2E.Infrastructure;

using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// Decorates the default <see cref="AuthenticationSchemeProvider"/> so that any lookup for the Google
/// OpenID Connect scheme resolves to <see cref="FakeExternalAuthenticationHandler"/> instead of the real
/// OIDC handler. Every other scheme (cookies, IdentityServer, ...) still resolves through the real,
/// unmodified provider — this only ever touches the one scheme name E2E tests need to fake.
/// </summary>
public sealed class FakeGoogleSchemeProvider(IOptions<AuthenticationOptions> options)
    : AuthenticationSchemeProvider(options)
{
    private readonly AuthenticationScheme _fakeGoogleScheme = new(
        GoogleOpenIdConnectDefaults.AuthenticationScheme,
        "Google",
        typeof(FakeExternalAuthenticationHandler));

    public override Task<AuthenticationScheme?> GetSchemeAsync(string name) =>
        IsGoogleScheme(name) ? Task.FromResult<AuthenticationScheme?>(_fakeGoogleScheme) : base.GetSchemeAsync(name);

    public override async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
    {
        var schemes = (await base.GetAllSchemesAsync()).ToList();
        for (var i = 0; i < schemes.Count; i++)
        {
            if (IsGoogleScheme(schemes[i].Name))
            {
                schemes[i] = _fakeGoogleScheme;
            }
        }

        return schemes;
    }

    private static bool IsGoogleScheme(string name) =>
        string.Equals(name, GoogleOpenIdConnectDefaults.AuthenticationScheme, StringComparison.Ordinal);
}
