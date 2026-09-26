namespace Identity.Tests.E2E.Infrastructure;

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
