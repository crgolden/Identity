namespace Identity.Tests.E2E.Synthetic;

using System.Text.Json;
using Microsoft.Playwright;

internal sealed record SyntheticAccount(
    string Email,
    string RpId,
    string CredentialId,
    string UserHandle,
    string PrivateKey)
{
    private const string LoginPath = "/Account/Login";
    private const float PasskeySubmitTimeoutMs = 15_000;
    private const float LoginTimeoutMs = 30_000;
    private const float AutofillGraceMs = 5_000;

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
            RequiredField(root, "id", credentialName),
            RequiredField(root, "userHandle", credentialName),
            RequiredField(root, "privateKey", credentialName));
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
        await page.GotoAsync($"{LoginPath}?ReturnUrl=%2F");
        if (await AutofillNavigatedAwayAsync(page))
        {
            return;
        }

        try
        {
            await page.FillAsync("input[name='Input.Email']", Email);
            await page.ClickAsync(PasskeySelectors.SignIn, new PageClickOptions { Timeout = PasskeySubmitTimeoutMs });
        }
        catch (Exception exception) when (exception is TimeoutException or PlaywrightException)
        {
            if (IsLoginPath(page.Url))
            {
                throw;
            }

            return;
        }

        await page.WaitForURLAsync(
            url => !IsLoginPath(url),
            new PageWaitForURLOptions { Timeout = LoginTimeoutMs });
    }

    private static string ToStandardBase64(string base64Url)
    {
        var padding = (4 - (base64Url.Length % 4)) % 4;
        return base64Url.Replace('-', '+').Replace('_', '/') + new string('=', padding);
    }

    private static long MonotonicSignCountSeed() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private async Task SeedPasskeyAsync(IPage page)
    {
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.SendAsync("WebAuthn.disable");
        await session.SendAsync("WebAuthn.enable");
        var authenticator = await session.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new Dictionary<string, object>
            {
                ["protocol"] = "ctap2",
                ["transport"] = "internal",
                ["hasResidentKey"] = true,
                ["hasUserVerification"] = true,
                ["isUserVerified"] = true,
                ["automaticPresenceSimulation"] = true,
            },
        });

        var authenticatorId = authenticator?.GetProperty("authenticatorId").GetString();
        if (string.IsNullOrWhiteSpace(authenticatorId))
        {
            throw new InvalidOperationException("WebAuthn.addVirtualAuthenticator returned no authenticatorId.");
        }

        await session.SendAsync("WebAuthn.addCredential", new Dictionary<string, object>
        {
            ["authenticatorId"] = authenticatorId,
            ["credential"] = new Dictionary<string, object>
            {
                ["credentialId"] = ToStandardBase64(CredentialId),
                ["isResidentCredential"] = true,
                ["rpId"] = RpId,
                ["privateKey"] = ToStandardBase64(PrivateKey),
                ["userHandle"] = ToStandardBase64(UserHandle),
                ["signCount"] = MonotonicSignCountSeed(),
            },
        });

        await CredentialSerialization.InstallAsync(page.Context);
    }

    private static async Task<bool> AutofillNavigatedAwayAsync(IPage page)
    {
        try
        {
            await page.WaitForURLAsync(
                url => !IsLoginPath(url),
                new PageWaitForURLOptions { Timeout = AutofillGraceMs });
            return true;
        }
        catch (Exception exception) when (exception is TimeoutException or PlaywrightException)
        {
            return false;
        }
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