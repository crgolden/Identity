namespace Identity.Tests.Extensions;

using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;
using Moq;

/// <summary>Unit tests for <see cref="ConfigurationExtensions"/> extension methods.</summary>
[Trait("Category", "Unit")]
public sealed class ConfigurationExtensionsTests
{
    /// <summary>
    /// Verifies that ToSecretClient returns a non-null SecretClient when a valid KeyVaultUri is present.
    /// Input: configuration with KeyVaultUri = "https://my-vault.vault.azure.net/" and a mock TokenCredential.
    /// Expected: a non-null SecretClient instance is returned.
    /// </summary>
    [Fact]
    public void ToSecretClient_ValidKeyVaultUri_ReturnsSecretClient()
    {
        // Arrange
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["KeyVaultUri"] = "https://my-vault.vault.azure.net/",
        });
        var credential = new Mock<TokenCredential>().Object;

        // Act
        var client = config.ToSecretClient(credential);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<SecretClient>(client);
    }

    /// <summary>
    /// Verifies that ToSecretClient throws InvalidOperationException when KeyVaultUri is missing from configuration.
    /// Input: empty configuration (no KeyVaultUri key).
    /// Expected: InvalidOperationException with message "Invalid 'KeyVaultUri'.".
    /// </summary>
    [Fact]
    public void ToSecretClient_MissingKeyVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = BuildConfig();
        var credential = new Mock<TokenCredential>().Object;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => config.ToSecretClient(credential));
        Assert.Equal("Invalid 'KeyVaultUri'.", ex.Message);
    }

    /// <summary>
    /// Verifies that ToSecretClient throws InvalidOperationException when KeyVaultUri is an empty string.
    /// Input: configuration with KeyVaultUri = "" (empty string cannot be parsed as Uri, binds as null).
    /// Expected: InvalidOperationException with message "Invalid 'KeyVaultUri'.".
    /// </summary>
    [Fact]
    public void ToSecretClient_EmptyKeyVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["KeyVaultUri"] = string.Empty,
        });
        var credential = new Mock<TokenCredential>().Object;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => config.ToSecretClient(credential));
        Assert.Equal("Invalid 'KeyVaultUri'.", ex.Message);
    }

    private static IConfiguration BuildConfig(IEnumerable<KeyValuePair<string, string?>>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? [])
            .Build();
}
