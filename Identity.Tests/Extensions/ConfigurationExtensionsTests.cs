namespace Identity.Tests.Extensions;

using Azure.Identity;
using Identity.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

[Trait("Category", "Unit")]
public class ConfigurationExtensionsTests
{
    [Fact]
    public void GetSections_ReturnsCorrectSectionKeys()
    {
        var config = BuildConfig();
        var (corsSection, sqlSection, credSection) = config.GetSections();
        Assert.Equal(nameof(CorsPolicy), corsSection.Key);
        Assert.Equal(nameof(SqlConnectionStringBuilder), sqlSection.Key);
        Assert.Equal(nameof(DefaultAzureCredentialOptions), credSection.Key);
    }

    [Fact]
    public void GetSections_EmptyConfiguration_StillReturnsSections()
    {
        var config = BuildConfig();
        var (corsSection, sqlSection, credSection) = config.GetSections();
        Assert.NotNull(corsSection);
        Assert.NotNull(sqlSection);
        Assert.NotNull(credSection);
    }

    [Fact]
    public void GetUris_AllValuesPresent_ReturnsUris()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ElasticsearchNode"] = "https://elasticsearch.example.com/",
            ["KeyVaultUri"] = "https://keyvault.example.com/",
            ["BlobUri"] = "https://blob.example.com/",
            ["DataProtectionKeyIdentifier"] = "https://keys.example.com/"
        });
        var (elasticsearch, keyVault, blob, dataProtection) = config.GetUris();
        Assert.Equal(new Uri("https://elasticsearch.example.com/"), elasticsearch);
        Assert.Equal(new Uri("https://keyvault.example.com/"), keyVault);
        Assert.Equal(new Uri("https://blob.example.com/"), blob);
        Assert.Equal(new Uri("https://keys.example.com/"), dataProtection);
    }

    [Theory]
    [InlineData("ElasticsearchNode", "Invalid 'ElasticsearchNode'.")]
    [InlineData("KeyVaultUri", "Invalid 'KeyVaultUri'.")]
    [InlineData("BlobUri", "Invalid 'BlobUri'.")]
    [InlineData("DataProtectionKeyIdentifier", "Invalid 'DataProtectionKeyIdentifier'.")]
    public void GetUris_MissingKey_ThrowsInvalidOperationException(string missingKey, string expectedMessage)
    {
        var values = new Dictionary<string, string?>
        {
            ["ElasticsearchNode"] = "https://elasticsearch.example.com/",
            ["KeyVaultUri"] = "https://keyvault.example.com/",
            ["BlobUri"] = "https://blob.example.com/",
            ["DataProtectionKeyIdentifier"] = "https://keys.example.com/"
        };
        values.Remove(missingKey);
        var config = BuildConfig(values);
        var ex = Assert.Throws<InvalidOperationException>(() => config.GetUris());
        Assert.Equal(expectedMessage, ex.Message);
    }

    private static IConfiguration BuildConfig(IEnumerable<KeyValuePair<string, string?>>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? [])
            .Build();
}
