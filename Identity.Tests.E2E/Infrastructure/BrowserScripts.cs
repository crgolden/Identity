namespace Identity.Tests.E2E.Infrastructure;

internal static class BrowserScripts
{
    internal static string CeremonyPrerequisites => Read(nameof(CeremonyPrerequisites));

    internal static string CredentialSerializationShim => Read(nameof(CredentialSerializationShim));

    internal static string DisableConditionalMediation => Read(nameof(DisableConditionalMediation));

    internal static string GrecaptchaStub => Read(nameof(GrecaptchaStub));

    internal static string LoginFormValidatorAttached => Read(nameof(LoginFormValidatorAttached));

    internal static string PasskeyRequestOptionsFetch => Read(nameof(PasskeyRequestOptionsFetch));

    private static string Read(string name)
    {
        using var stream = typeof(BrowserScripts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"No embedded browser script named '{name}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
