namespace Identity.Tests.Integration.Security;

using System.Net;
using Identity.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class ConcurrentLockoutTests(IntegrationFixture fixture)
{
    [Fact]
    public async Task Login_ConcurrentFailedAttempts_NeverSignInAndNeverFailTheServer()
    {
        var (email, _) = await fixture.CreateConfirmedUserAsync();
        var wrongPassword = Generated.NewPassword();
        var maxFailedAttempts = fixture.Factory.Services
            .GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;
        var attemptsPerThreshold = Random.Shared.Next(2, 4);
        (HttpStatusCode Status, string? RedirectPath)[] acceptedOutcomes =
        [
            (HttpStatusCode.OK, null),
            (HttpStatusCode.Redirect, PageRoutes.Lockout),
        ];

        var outcomes = await Task.WhenAll(Enumerable.Range(0, maxFailedAttempts * attemptsPerThreshold).Select(async _ =>
        {
            using var attempt = AccountSession.NewClient(fixture);
            using var response = await AccountSession.SubmitLoginAsync(fixture, attempt, email, wrongPassword);
            return (response.StatusCode, AccountSession.RedirectPath(attempt, response));
        }));

        Assert.All(outcomes, outcome => Assert.Contains(outcome, acceptedOutcomes));
    }
}
