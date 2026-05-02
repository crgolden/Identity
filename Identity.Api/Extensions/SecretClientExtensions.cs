namespace Identity.Extensions;

using Azure.Security.KeyVault.Secrets;

public static class SecretClientExtensions
{
    extension(SecretClient secretClient)
    {
#pragma warning disable SA1009
        public (
            KeyVaultSecret GoogleClientId,
            KeyVaultSecret GoogleClientSecret,
            KeyVaultSecret ResendApiToken,
            KeyVaultSecret GravatarApiSecretKey,
            KeyVaultSecret SqlServerUserId,
            KeyVaultSecret SqlServerPassword,
            KeyVaultSecret ElasticsearchUsername,
            KeyVaultSecret ElasticsearchPassword
        ) GetIdentitySecrets()
        {
            var googleClientId = secretClient.GetSecret("GoogleClientId");
            var googleClientSecret = secretClient.GetSecret("GoogleClientSecret");
            var resendApiToken = secretClient.GetSecret("ResendApiToken");
            var gravatarApiSecretKey = secretClient.GetSecret("GravatarApiSecretKey");
            var sqlServerUserId = secretClient.GetSecret("SqlServerUserId");
            var sqlServerPassword = secretClient.GetSecret("SqlServerPassword");
            var elasticsearchUsername = secretClient.GetSecret("ElasticsearchUsername");
            var elasticsearchPassword = secretClient.GetSecret("ElasticsearchPassword");
            return (
                googleClientId.Value,
                googleClientSecret.Value,
                resendApiToken.Value,
                gravatarApiSecretKey.Value,
                sqlServerUserId.Value,
                sqlServerPassword.Value,
                elasticsearchUsername.Value,
                elasticsearchPassword.Value
            );
        }
#pragma warning restore SA1009
    }
}