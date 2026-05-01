namespace Identity.Extensions;

using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
#pragma warning disable SA1009
        internal (
            KeyVaultSecret GoogleClientId,
            KeyVaultSecret GoogleClientSecret,
            KeyVaultSecret ResendApiToken,
            KeyVaultSecret GravatarApiSecretKey,
            KeyVaultSecret ElasticsearchUsername,
            KeyVaultSecret ElasticsearchPassword,
            KeyVaultSecret SqlServerUserId,
            KeyVaultSecret SqlServerPassword
        ) GetSecrets(Uri keyVaultUrl, TokenCredential tokenCredential)
        {
            var defaultAzureCredentialOptionsSection = builder.Configuration.GetSection(nameof(DefaultAzureCredentialOptions));
            if (!defaultAzureCredentialOptionsSection.Exists())
            {
                throw new InvalidOperationException($"Missing '{nameof(DefaultAzureCredentialOptions)}' section.");
            }

            var secretClient = new SecretClient(keyVaultUrl, tokenCredential);
            return (
                secretClient.GetSecret("GoogleClientId").Value,
                secretClient.GetSecret("GoogleClientSecret").Value,
                secretClient.GetSecret("ResendApiToken").Value,
                secretClient.GetSecret("GravatarApiSecretKey").Value,
                secretClient.GetSecret("ElasticsearchUsername").Value,
                secretClient.GetSecret("ElasticsearchPassword").Value,
                secretClient.GetSecret("SqlServerUserId").Value,
                secretClient.GetSecret("SqlServerPassword").Value
            );
        }
#pragma warning restore SA1009
    }
}
