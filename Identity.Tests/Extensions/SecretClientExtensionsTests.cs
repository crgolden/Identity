namespace Identity.Tests.Extensions;

using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Identity.Extensions;

[Trait("Category", "Unit")]
public class SecretClientExtensionsTests
{
    private static readonly string[] SecretNames =
    [
        "GravatarApiSecretKey",
        "ElasticsearchUsername",
        "ElasticsearchPassword",
        "SqlServerUserId",
        "SqlServerPassword",
        "GoogleClientId",
        "GoogleClientSecret",
        "ResendApiToken"
    ];

    [Fact]
    public async Task GetSecrets_AllSecretsPresent_ReturnsAllValues()
    {
        var client = new FakeSecretClient();
        var (s0, s1, s2, s3, s4, s5, s6, s7) = await client.GetSecrets(TestContext.Current.CancellationToken);
        Assert.Equal("GravatarApiSecretKey-value", s0.Value);
        Assert.Equal("ElasticsearchUsername-value", s1.Value);
        Assert.Equal("ElasticsearchPassword-value", s2.Value);
        Assert.Equal("SqlServerUserId-value", s3.Value);
        Assert.Equal("SqlServerPassword-value", s4.Value);
        Assert.Equal("GoogleClientId-value", s5.Value);
        Assert.Equal("GoogleClientSecret-value", s6.Value);
        Assert.Equal("ResendApiToken-value", s7.Value);
    }

    [Fact]
    public async Task GetSecrets_ReturnsSecretsInExpectedTupleOrder()
    {
        var client = new FakeSecretClient();
        var (s0, s1, s2, s3, s4, s5, s6, s7) = await client.GetSecrets(TestContext.Current.CancellationToken);
        Assert.Equal("GravatarApiSecretKey", s0.Name);
        Assert.Equal("ElasticsearchUsername", s1.Name);
        Assert.Equal("ElasticsearchPassword", s2.Name);
        Assert.Equal("SqlServerUserId", s3.Name);
        Assert.Equal("SqlServerPassword", s4.Name);
        Assert.Equal("GoogleClientId", s5.Name);
        Assert.Equal("GoogleClientSecret", s6.Name);
        Assert.Equal("ResendApiToken", s7.Name);
    }

    [Fact]
    public async Task GetSecrets_FetchesAllEightSecretsByName()
    {
        var client = new FakeSecretClient();
        await client.GetSecrets(TestContext.Current.CancellationToken);
        Assert.Equal(SecretNames.Length, client.Calls.Count);
        foreach (var name in SecretNames)
        {
            Assert.Contains(client.Calls, c => c.Name == name);
        }
    }

    [Fact]
    public async Task GetSecrets_PassesCancellationToken()
    {
        var token = TestContext.Current.CancellationToken;
        var client = new FakeSecretClient();
        await client.GetSecrets(token);
        Assert.All(client.Calls, c => Assert.Equal(token, c.Token));
    }

    [Fact]
    public async Task GetSecrets_OneSecretThrows_PropagatesException()
    {
        var client = new FakeSecretClient(throwOn: "SqlServerPassword");
        await Assert.ThrowsAsync<RequestFailedException>(
            () => client.GetSecrets(TestContext.Current.CancellationToken));
    }

    private sealed class FakeSecretClient : SecretClient
    {
        private readonly string? _throwOn;

        public FakeSecretClient(string? throwOn = null) => _throwOn = throwOn;

        public List<(string Name, CancellationToken Token)> Calls { get; } = [];

        public override Task<Response<KeyVaultSecret>> GetSecretAsync(
            string name,
            string? version = null,
            SecretContentType? outContentType = null,
            CancellationToken cancellationToken = default)
        {
            if (name == _throwOn)
            {
                return Task.FromException<Response<KeyVaultSecret>>(new RequestFailedException("Secret not found"));
            }

            Calls.Add((name, cancellationToken));
            return Task.FromResult(Response.FromValue(new KeyVaultSecret(name, $"{name}-value"), new FakeHttpResponse()));
        }
    }

    private sealed class FakeHttpResponse : Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream
        {
            get => null;
            set => _ = value;
        }

        public override string ClientRequestId
        {
            get => string.Empty;
            set => _ = value;
        }

        public override void Dispose()
        {
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            values = null;
            return false;
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
    }
}
