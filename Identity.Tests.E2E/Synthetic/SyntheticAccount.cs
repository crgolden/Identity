namespace Identity.Tests.E2E.Synthetic;

using System.Text.Json;
using Microsoft.Playwright;

internal sealed record SyntheticAccount(string Email, string RpId, CredentialsCreateOptions Credential)
{
    private const string LoginPath = "/Account/Login";
    private const string PasskeySubmitSelector = "#passkey-submit";
    private const float PasskeySubmitTimeoutMs = 15_000;
    private const float LoginTimeoutMs = 30_000;

    public static SyntheticAccount Resolve(int slot)
    {
        var emailName = $"EMAIL{slot}";
        var credentialName = $"PASSKEY_CREDENTIAL{slot}";
        var email = Environment.GetEnvironmentVariable(emailName);
        var rawCredential = Environment.GetEnvironmentVariable(credentialName);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rawCredential))
        {
            throw new InvalidOperationException(
                $"{emailName} and {credentialName} must both be set for a synthetic walk.");
        }

        using var document = JsonDocument.Parse(rawCredential);
        var root = document.RootElement;
        return new SyntheticAccount(
            email,
            RequiredField(root, "rpId", credentialName),
            new CredentialsCreateOptions
            {
                Id = RequiredField(root, "id", credentialName),
                UserHandle = RequiredField(root, "userHandle", credentialName),
                PrivateKey = RequiredField(root, "privateKey", credentialName),
                PublicKey = RequiredField(root, "publicKey", credentialName),
            });
    }

    public static async Task SignOutAsync(IPage page)
    {
        await page.GotoAsync("/");
        await page.ClickAsync("#logout-submit");
        await page.WaitForSelectorAsync("#login-nav");
    }

    public async Task SignInAsync(IPage page)
    {
        await page.Context.Credentials.CreateAsync(RpId, Credential);
        await page.Context.Credentials.InstallAsync();
        await page.GotoAsync($"{LoginPath}?ReturnUrl=%2F");
        await page.FillAsync("input[name='Input.Email']", Email);
        try
        {
            await page.ClickAsync(PasskeySubmitSelector, new PageClickOptions { Timeout = PasskeySubmitTimeoutMs });
        }
        catch (PlaywrightException) when (!IsOnLoginPage(page))
        {
            return;
        }

        await page.WaitForURLAsync(
            url => !IsLoginPath(url),
            new PageWaitForURLOptions { Timeout = LoginTimeoutMs });
    }

    private static bool IsOnLoginPage(IPage page) => IsLoginPath(page.Url);

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