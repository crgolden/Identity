namespace Identity.Extensions;

using Azure.Identity;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Data.SqlClient;

/// <summary>Provides strongly-typed configuration accessors for <see cref="IConfiguration"/>.</summary>
public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        /// <summary>Retrieves the CorsPolicy, SqlConnectionStringBuilder, and DefaultAzureCredentialOptions configuration sections.</summary>
        /// <returns>A tuple of the CorsPolicy, SqlConnectionStringBuilder, and DefaultAzureCredentialOptions <see cref="IConfigurationSection"/> instances.</returns>
        public (IConfigurationSection, IConfigurationSection, IConfigurationSection) GetSections()
        {
            var corsPolicySection = configuration.GetSection(nameof(CorsPolicy));
            var sqlConnectionStringBuilderSection = configuration.GetSection(nameof(SqlConnectionStringBuilder));
            var defaultAzureCredentialOptionsSection = configuration.GetSection(nameof(DefaultAzureCredentialOptions));
            return (corsPolicySection, sqlConnectionStringBuilderSection, defaultAzureCredentialOptionsSection);
        }

        /// <summary>Retrieves the Elasticsearch node, Key Vault, Blob Storage, and Data Protection key URIs from configuration.</summary>
        /// <returns>A tuple of the ElasticsearchNode, KeyVaultUri, BlobUri, and DataProtectionKeyIdentifier <see cref="Uri"/> values.</returns>
        /// <exception cref="InvalidOperationException">Thrown when any required URI configuration value is missing or invalid.</exception>
        public (Uri, Uri, Uri, Uri) GetUris()
        {
            var elasticsearchNode = configuration.GetValue<Uri>("ElasticsearchNode") ?? throw new InvalidOperationException("Invalid 'ElasticsearchNode'.");
            var keyVaultUrl = configuration.GetValue<Uri>("KeyVaultUri") ?? throw new InvalidOperationException("Invalid 'KeyVaultUri'.");
            var blobUrl = configuration.GetValue<Uri>("BlobUri") ?? throw new InvalidOperationException("Invalid 'BlobUri'.");
            var dataProtectionKeyIdentifier = configuration.GetValue<Uri>("DataProtectionKeyIdentifier") ?? throw new InvalidOperationException("Invalid 'DataProtectionKeyIdentifier'.");
            return (elasticsearchNode, keyVaultUrl, blobUrl, dataProtectionKeyIdentifier);
        }
    }
}
