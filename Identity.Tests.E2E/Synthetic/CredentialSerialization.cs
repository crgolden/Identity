namespace Identity.Tests.E2E.Synthetic;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

internal static class CredentialSerialization
{
    public static Task InstallAsync(IBrowserContext context) =>
        context.AddInitScriptAsync(BrowserScripts.CredentialSerializationShim);
}
