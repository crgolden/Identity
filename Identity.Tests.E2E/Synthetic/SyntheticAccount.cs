namespace Identity.Tests.E2E.Synthetic;

using System.Text.Json;
using Microsoft.Playwright;
using static Identity.Tests.E2E.Synthetic.SyntheticAccountConstants;

internal sealed record SyntheticAccount(
    string RpId,
    string CredentialId,
    string UserHandle,
    string PrivateKey)
{
    private const string LoginPath = PageRoutes.Login;

    public static SyntheticAccount Resolve(int slot)
    {
        var credentialName = $"{CredentialVariablePrefix}{slot}";
        var rawCredential = Environment.GetEnvironmentVariable(credentialName);
        if (string.IsNullOrWhiteSpace(rawCredential))
        {
            throw new InvalidOperationException($"{credentialName} must be set for a synthetic walk.");
        }

        using var document = JsonDocument.Parse(rawCredential);
        var root = document.RootElement;
        return new SyntheticAccount(
            RequiredField(root, WebAuthnCdpConstants.RpIdKey, credentialName),
            RequiredField(root, WebAuthnCdpConstants.StoredCredentialIdField, credentialName),
            RequiredField(root, WebAuthnCdpConstants.UserHandleKey, credentialName),
            RequiredField(root, WebAuthnCdpConstants.PrivateKeyKey, credentialName));
    }

    public static async Task SignOutAsync(IPage page)
    {
        await page.GotoAsync("/");
        await page.ClickAsync("#logout-submit");
        await page.WaitForSelectorAsync("#login-nav");
    }

    public async Task SignInAsync(IPage page)
    {
        await SeedPasskeyAsync(page);
        await page.GotoAsync($"{LoginPath}{ReturnToRootQuery}");
        await page.WaitForURLAsync(url => !IsLoginPath(url));
    }

    private static string ToStandardBase64(string base64Url)
    {
        var padding = (Base64QuantumLength - (base64Url.Length % Base64QuantumLength)) % Base64QuantumLength;
        return base64Url.Replace('-', '+').Replace('_', '/') + new string('=', padding);
    }

    private static long MonotonicSignCountSeed() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private async Task SeedPasskeyAsync(IPage page)
    {
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync(WebAuthnCdpConstants.DisableMethod);
        await session.SendAsync(WebAuthnCdpConstants.EnableMethod);
        var authenticator = await session.SendAsync(
            WebAuthnCdpConstants.AddVirtualAuthenticatorMethod,
            new Dictionary<string, object>
            {
                [WebAuthnCdpConstants.OptionsKey] = new Dictionary<string, object>
                {
                    [WebAuthnCdpConstants.ProtocolKey] = WebAuthnCdpConstants.Ctap2Protocol,
                    [WebAuthnCdpConstants.TransportKey] = WebAuthnCdpConstants.InternalTransport,
                    [WebAuthnCdpConstants.HasResidentKeyKey] = true,
                    [WebAuthnCdpConstants.HasUserVerificationKey] = true,
                    [WebAuthnCdpConstants.IsUserVerifiedKey] = true,
                    [WebAuthnCdpConstants.AutomaticPresenceSimulationKey] = true,
                },
            });

        var authenticatorId = authenticator?.GetProperty(WebAuthnCdpConstants.AuthenticatorIdKey).GetString();
        if (string.IsNullOrWhiteSpace(authenticatorId))
        {
            throw new InvalidOperationException("WebAuthn.addVirtualAuthenticator returned no authenticatorId.");
        }

        await session.SendAsync(
            WebAuthnCdpConstants.AddCredentialMethod,
            new Dictionary<string, object>
            {
                [WebAuthnCdpConstants.AuthenticatorIdKey] = authenticatorId,
                [WebAuthnCdpConstants.CredentialKey] = new Dictionary<string, object>
                {
                    [WebAuthnCdpConstants.CredentialIdKey] = ToStandardBase64(CredentialId),
                    [WebAuthnCdpConstants.IsResidentCredentialKey] = true,
                    [WebAuthnCdpConstants.RpIdKey] = RpId,
                    [WebAuthnCdpConstants.PrivateKeyKey] = ToStandardBase64(PrivateKey),
                    [WebAuthnCdpConstants.UserHandleKey] = ToStandardBase64(UserHandle),
                    [WebAuthnCdpConstants.SignCountKey] = MonotonicSignCountSeed(),
                },
            });

        await CredentialSerialization.InstallAsync(page.Context);
    }

    private static bool IsLoginPath(string url) =>
        new Uri(url).AbsolutePath.StartsWith(LoginPath, StringComparison.OrdinalIgnoreCase);

    private static string RequiredField(JsonElement root, string name, string envName)
    {
        if (root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"{envName} is missing the non-empty string field '{name}'.");
    }
}
