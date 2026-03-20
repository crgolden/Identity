namespace Identity.Extensions;

using Azure.Identity;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Data.SqlClient;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public (IConfigurationSection, IConfigurationSection, IConfigurationSection) GetSections()
        {
            var corsPolicySection = configuration.GetSection(nameof(CorsPolicy));
            var sqlConnectionStringBuilderSection = configuration.GetSection(nameof(SqlConnectionStringBuilder));
            var defaultAzureCredentialOptionsSection = configuration.GetSection(nameof(DefaultAzureCredentialOptions));
            return (corsPolicySection, sqlConnectionStringBuilderSection, defaultAzureCredentialOptionsSection);
        }

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
