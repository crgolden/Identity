using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Identity;

public class OpenIdConnectConfigureOptions : IPostConfigureOptions<OpenIdConnectOptions>
{
    private readonly SecretClient _secretClient;

    public OpenIdConnectConfigureOptions(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public void PostConfigure(string? name, OpenIdConnectOptions options)
    {
        switch (name)
        {
            case GoogleOpenIdConnectDefaults.AuthenticationScheme:
                var googleClientId = _secretClient.GetSecret("GoogleClientId").Value;
                var googleClientSecret = _secretClient.GetSecret("GoogleClientSecret").Value;
                options.ClientId = googleClientId.Value;
                options.ClientSecret = googleClientSecret.Value;
                return;
        }
    }
}
