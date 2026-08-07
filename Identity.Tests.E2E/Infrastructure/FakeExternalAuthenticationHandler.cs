namespace Identity.Tests.E2E.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
    public const string ClaimsCookieName = "E2E-Google-Claims";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Request.Cookies.TryGetValue(ClaimsCookieName, out var json) || string.IsNullOrWhiteSpace(json))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync($"Missing '{ClaimsCookieName}' cookie for fake external login.");
            return;
        }

        var payload = JsonSerializer.Deserialize<FakeGoogleClaims>(Uri.UnescapeDataString(json))
            ?? throw new InvalidOperationException($"Invalid '{ClaimsCookieName}' cookie payload.");

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, payload.Sub ?? Guid.NewGuid().ToString()) };
        if (payload.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, payload.Email));
        }

        if (payload.EmailVerified.HasValue)
        {
            claims.Add(new Claim("email_verified", payload.EmailVerified.Value ? "true" : "false"));
        }

        if (payload.Name is not null)
        {
            claims.Add(new Claim("name", payload.Name));
        }

        if (payload.Picture is not null)
        {
            claims.Add(new Claim("picture", payload.Picture));
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

public sealed class FakeGoogleClaims
{
    public string? Sub { get; set; }

    public string? Email { get; set; }

    public bool? EmailVerified { get; set; }

    public string? Name { get; set; }

    public string? Picture { get; set; }

    public string? GivenName { get; set; }

    public string? Surname { get; set; }
}