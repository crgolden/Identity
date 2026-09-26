namespace Identity.Tests.E2E.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Identity.Avatar;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class FakeExternalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public static readonly string ClaimsCookieName = Guid.NewGuid().ToString("N");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Request.Cookies.TryGetValue(ClaimsCookieName, out var json) || string.IsNullOrWhiteSpace(json))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var payload = JsonSerializer.Deserialize<FakeGoogleClaims>(Uri.UnescapeDataString(json))
            ?? throw new InvalidOperationException($"Invalid '{ClaimsCookieName}' cookie payload.");

        var subject = payload.Sub ?? Generated.NewExternalSubject();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, subject) };
        if (payload.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, payload.Email));
        }

        if (payload.EmailVerified.HasValue)
        {
            var emailVerified = payload.EmailVerified.Value
                ? OidcStandardConstants.ClaimValueTrue
                : OidcStandardConstants.ClaimValueFalse;
            claims.Add(new Claim(ExternalLogin.EmailVerifiedClaimType, emailVerified));
        }

        if (payload.Name is not null)
        {
            claims.Add(new Claim(OidcStandardConstants.NameClaim, payload.Name));
        }

        if (payload.Picture is not null)
        {
            claims.Add(new Claim(AvatarProfileService.PictureClaimType, payload.Picture));
        }

        if (payload.GivenName is not null)
        {
            claims.Add(new Claim(ClaimTypes.GivenName, payload.GivenName));
        }

        if (payload.Surname is not null)
        {
            claims.Add(new Claim(ClaimTypes.Surname, payload.Surname));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        await Context.SignInAsync(IdentityConstants.ExternalScheme, principal, properties);
        Response.Redirect(properties.RedirectUri ?? "/");
    }
}
