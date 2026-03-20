namespace Identity.Extensions;

using Azure.Security.KeyVault.Secrets;

public static class SecretClientExtensions
{
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
