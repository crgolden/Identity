using Azure;
using Azure.Security.KeyVault.Secrets;
using Moq;
using Resend;

namespace Identity.Tests;

public class ResendClientConfigureOptionsTests
{
    /// <summary>
    /// Verifies that PostConfigure sets the ApiToken on the provided options
    /// by retrieving the secret named "ResendApiToken" from Key Vault.
    /// Tests multiple 'name' inputs (including null) and secret values (empty, normal, long, special).
    /// Expectation: options.ApiToken equals the secret's Value after PostConfigure.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidSecretCases))]
    public void PostConfigure_NameVariations_SetsApiToken(string? name, string secretValue)
    {
        // Arrange
        var vaultUri = new Uri("https://example.vault");
        var credential = Mock.Of<global::Azure.Core.TokenCredential>();
        var mockSecretClient = new Mock<SecretClient>(MockBehavior.Strict, vaultUri, credential);

        var kvSecret = new KeyVaultSecret("ResendApiToken", secretValue);
        var mockResponse = new Mock<Response<KeyVaultSecret>>(MockBehavior.Strict);
        mockResponse.SetupGet(r => r.Value).Returns(kvSecret);

        mockSecretClient
            .Setup(c => c.GetSecret(It.Is<string>(s => s == "ResendApiToken")))
            .Returns(mockResponse.Object);

        var sut = new ResendClientConfigureOptions(mockSecretClient.Object);
        var options = new ResendClientOptions();

        // Act
        sut.PostConfigure(name, options);

        // Assert
        Assert.Equal(secretValue, options.ApiToken);
    }

    /// <summary>
    /// Ensures that PostConfigure will overwrite an existing ApiToken value on the options.
    /// Input: options.ApiToken initially set to a non-empty value; Key Vault returns a different token.
    /// Expectation: ApiToken is replaced with the KeyVault secret Value.
    /// </summary>
    [Fact]
    public void PostConfigure_ExistingApiToken_IsOverwritten()
    {
        // Arrange
        var vaultUri = new Uri("https://example.vault");
        var credential = Mock.Of<global::Azure.Core.TokenCredential>();
        var mockSecretClient = new Mock<SecretClient>(MockBehavior.Strict, vaultUri, credential);

        var newToken = "new-secret-token";
        var kvSecret = new KeyVaultSecret("ResendApiToken", newToken);
        var mockResponse = new Mock<Response<KeyVaultSecret>>(MockBehavior.Strict);
        mockResponse.SetupGet(r => r.Value).Returns(kvSecret);

        mockSecretClient
            .Setup(c => c.GetSecret(It.Is<string>(s => s == "ResendApiToken")))
            .Returns(mockResponse.Object);

        var sut = new ResendClientConfigureOptions(mockSecretClient.Object);
        var options = new ResendClientOptions { ApiToken = "old-token" };

        // Act
        sut.PostConfigure("anyName", options);

        // Assert
        Assert.Equal(newToken, options.ApiToken);
    }

    /// <summary>
    /// Verifies that PostConfigure propagates exceptions thrown by the SecretClient.GetSecret call.
    /// Input: SecretClient.GetSecret throws InvalidOperationException.
    /// Expectation: the same InvalidOperationException is thrown by PostConfigure.
    /// </summary>
    [Fact]
    public void PostConfigure_GetSecretThrows_ExceptionPropagated()
    {
        // Arrange
        var vaultUri = new Uri("https://example.vault");
        var credential = Mock.Of<global::Azure.Core.TokenCredential>();
        var mockSecretClient = new Mock<SecretClient>(MockBehavior.Strict, vaultUri, credential);

        mockSecretClient
            .Setup(c => c.GetSecret(It.Is<string>(s => s == "ResendApiToken")))
            .Throws(new InvalidOperationException("KV failure"));

        var sut = new ResendClientConfigureOptions(mockSecretClient.Object);
        var options = new ResendClientOptions();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => sut.PostConfigure(null, options));
        Assert.Equal("KV failure", ex.Message);
    }

    // Member data for parameterized test cases:
    public static IEnumerable<object?[]> ValidSecretCases()
    {
        yield return new object?[] { null, "token-123" };
        yield return new object?[] { "", "" }; // empty secret
        yield return new object?[] { " ", "   " }; // whitespace secret
        yield return new object?[] { "customName", new string('x', 1000) }; // long secret
        yield return new object?[] { "nameWithSpecial\n\t", "spec!@#\r\n" }; // special characters
    }

    /// <summary>
    /// Constructor_SecretClientProvided_InstanceCreated
    /// Purpose: Verify that the constructor can be invoked when a valid <see cref="SecretClient"/> is provided.
    /// Conditions: No concrete <see cref="SecretClient"/> factory or easily mockable implementation is available in the test runtime.
    /// Expected: The constructor should not throw when provided a valid SecretClient; if a test runtime cannot provide one, this test is marked skipped and documents next steps.
    /// </summary>
    [Fact]
    public void Constructor_SecretClientProvided_InstanceCreated()
    {
        // Arrange
        var vaultUri = new Uri("https://example.vault");
        var credential = Mock.Of<global::Azure.Core.TokenCredential>();
        var mockSecretClient = new Mock<SecretClient>(MockBehavior.Strict, vaultUri, credential);

        // Act
        var options = new ResendClientConfigureOptions(mockSecretClient.Object);

        // Assert
        Assert.NotNull(options);
    }
}