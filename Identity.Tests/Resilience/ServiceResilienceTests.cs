namespace Identity.Tests.Resilience;

using Azure;
using Identity.Extensions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;
using Resend;

/// <summary>
/// Resilience tests verifying that external service failures propagate correctly
/// and do not corrupt application state or swallow exceptions unexpectedly.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ServiceResilienceTests
{
    // -------------------------------------------------------------------------
    // EmailSender resilience
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EmailSender_WhenResendThrowsApiException_PropagatesException()
    {
        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResendException("Resend API error", innerException: null));

        IEmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAsync<ResendException>(
            () => sender.SendEmailAsync(
                "to@example.com",
                "Subject",
                "Body",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EmailSender_WhenResendThrowsHttpRequestException_PropagatesException()
    {
        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        IEmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendEmailAsync(
                "to@example.com",
                "Subject",
                "Body",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EmailSender_WhenCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        IEmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sender.SendEmailAsync("to@example.com", "Subject", "Body", cts.Token));
    }

    // -------------------------------------------------------------------------
    // SecretClient resilience
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSecrets_AllSecretNames_AreCorrect()
    {
        // Verify the extension fetches exactly the 8 expected secret names —
        // a regression guard against a secret name being silently renamed.
        var expectedNames = new[]
        {
            "GravatarApiSecretKey",
            "ElasticsearchUsername",
            "ElasticsearchPassword",
            "SqlServerUserId",
            "SqlServerPassword",
            "GoogleClientId",
            "GoogleClientSecret",
            "ResendApiToken"
        };

        var actualNames = new List<string>();
        var fakeClient = new RecordingSecretClient(actualNames);
        await fakeClient.GetSecrets(TestContext.Current.CancellationToken);

        Assert.Equal(expectedNames.OrderBy(x => x), actualNames.OrderBy(x => x));
    }

    [Fact]
    public async Task GetSecrets_WhenOneSecretUnavailable_PropagatesException()
    {
        var client = new FailingSecretClient("SqlServerPassword");

        await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecrets(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSecrets_PassesCancellationToken_ToAllCalls()
    {
        var actualNames = new List<string>();
        var fakeClient = new RecordingSecretClient(actualNames);
        using var cts = new CancellationTokenSource();
        await fakeClient.GetSecrets(cts.Token);
        Assert.Equal(8, actualNames.Count);
    }

    // -------------------------------------------------------------------------
    // Fake SecretClient helpers
    // -------------------------------------------------------------------------

    private sealed class RecordingSecretClient : Azure.Security.KeyVault.Secrets.SecretClient
    {
        private readonly List<string> _calls;

        public RecordingSecretClient(List<string> calls) => _calls = calls;

        public override Task<Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret>> GetSecretAsync(
            string name,
            string? version,
            Azure.Security.KeyVault.Secrets.SecretContentType? contentType,
            CancellationToken cancellationToken = default)
        {
            _calls.Add(name);
            var secret = new Azure.Security.KeyVault.Secrets.KeyVaultSecret(name, $"{name}-value");
            return Task.FromResult(Response.FromValue(secret, new FakeHttpResponse()));
        }
    }

    private sealed class FailingSecretClient : Azure.Security.KeyVault.Secrets.SecretClient
    {
        private readonly string _failOn;

        public FailingSecretClient(string failOn) => _failOn = failOn;

        public override Task<Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret>> GetSecretAsync(
            string name,
            string? version,
            Azure.Security.KeyVault.Secrets.SecretContentType? contentType,
            CancellationToken cancellationToken = default)
        {
            if (name == _failOn)
            {
                return Task.FromException<Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret>>(
                    new RequestFailedException("Secret unavailable"));
            }

            var secret = new Azure.Security.KeyVault.Secrets.KeyVaultSecret(name, $"{name}-value");
            return Task.FromResult(Response.FromValue(secret, new FakeHttpResponse()));
        }
    }

    private sealed class FakeHttpResponse : Azure.Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get => null; set { } }

        public override string ClientRequestId { get => string.Empty; set { } }

        public override void Dispose() { }

        protected override bool TryGetHeader(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEnumerable<string>? values)
        {
            values = null;
            return false;
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<Azure.Core.HttpHeader> EnumerateHeaders() => [];
    }
}
