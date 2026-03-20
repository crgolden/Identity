namespace Identity.Extensions;

using Azure.Security.KeyVault.Secrets;

/// <summary>Provides batch Key Vault secret retrieval for <see cref="SecretClient"/>.</summary>
public static class SecretClientExtensions
{
    /// <summary>Concurrently fetches all required application secrets from Azure Key Vault.</summary>
    /// <param name="secretClient">The Key Vault <see cref="SecretClient"/> to use.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that resolves to an 8-tuple of <see cref="KeyVaultSecret"/> values for GravatarApiSecretKey, ElasticsearchUsername, ElasticsearchPassword, SqlServerUserId, SqlServerPassword, GoogleClientId, GoogleClientSecret, and ResendApiToken respectively.</returns>
    public static async Task<(KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret)> GetSecrets(this SecretClient secretClient, CancellationToken cancellationToken = default)
    {
        var tasks = new[]
        {
            secretClient.GetSecretAsync("GravatarApiSecretKey", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("ElasticsearchUsername", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("ElasticsearchPassword", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("SqlServerUserId", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("SqlServerPassword", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("GoogleClientId", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("GoogleClientSecret", cancellationToken: cancellationToken),
            secretClient.GetSecretAsync("ResendApiToken", cancellationToken: cancellationToken)
        };
        var result = await Task.WhenAll(tasks);
        return (result[0].Value, result[1].Value, result[2].Value, result[3].Value, result[4].Value, result[5].Value, result[6].Value, result[7].Value);
    }
}
