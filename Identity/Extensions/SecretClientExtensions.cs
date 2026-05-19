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
            KeyVaultSecret ServiceBusNamespace,
            KeyVaultSecret GravatarApiSecretKey,
            KeyVaultSecret SqlServerUserId,
            KeyVaultSecret SqlServerPassword,
            KeyVaultSecret ElasticsearchUsername,
            KeyVaultSecret ElasticsearchPassword,
            KeyVaultSecret ReCAPTCHASiteKey,
            KeyVaultSecret ReCAPTCHASecretKey,
            KeyVaultSecret AdminEmail,
            KeyVaultSecret TestEmail
        ) GetIdentitySecrets()
        {
            var googleClientId = secretClient.GetSecret("GoogleClientId");
            var googleClientSecret = secretClient.GetSecret("GoogleClientSecret");
            var serviceBusNamespace = secretClient.GetSecret("ServiceBusNamespace");
            var gravatarApiSecretKey = secretClient.GetSecret("GravatarApiSecretKey");
            var sqlServerUserId = secretClient.GetSecret("SqlServerUserId");
            var sqlServerPassword = secretClient.GetSecret("SqlServerPassword");
            var elasticsearchUsername = secretClient.GetSecret("ElasticsearchUsername");
            var elasticsearchPassword = secretClient.GetSecret("ElasticsearchPassword");
            var recaptchaSiteKey = secretClient.GetSecret("ReCAPTCHASiteKey");
            var recaptchaSecretKey = secretClient.GetSecret("ReCAPTCHASecretKey");
            var adminEmail = secretClient.GetSecret("AdminEmail");
            var testEmail = secretClient.GetSecret("TestEmail");
            return (
                googleClientId.Value,
                googleClientSecret.Value,
                serviceBusNamespace.Value,
                gravatarApiSecretKey.Value,
                sqlServerUserId.Value,
                sqlServerPassword.Value,
                elasticsearchUsername.Value,
                elasticsearchPassword.Value,
                recaptchaSiteKey.Value,
                recaptchaSecretKey.Value,
                adminEmail.Value,
                testEmail.Value
            );
        }
#pragma warning restore SA1009
    }
}
