using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Resend;

namespace Identity;

public class ResendClientConfigureOptions : IPostConfigureOptions<ResendClientOptions>
{
    private readonly SecretClient _secretClient;

    public ResendClientConfigureOptions(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public void PostConfigure(string? name, ResendClientOptions options)
    {
        var resendApiToken = _secretClient.GetSecret("ResendApiToken").Value;
        options.ApiToken = resendApiToken.Value;
    }
}
