namespace Identity.Tests.Unit.Infrastructure;

internal static class TestValues
{
    internal static string LowercaseToken(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('a', 'z' + 1)));

    internal static string NewEmailAddress() => $"{LowercaseToken(8)}@{LowercaseToken(10)}.example";

    internal static string NewUserName() => LowercaseToken(9);

    internal static string NewPassword() => $"{LowercaseToken(10)}-{Random.Shared.Next(1000, 10000)}!Aa";

    internal static string NewProviderKey() => LowercaseToken(16);

    internal static string NewFailureReason() => $"failure-{LowercaseToken(10)}";

    internal static string NewClaimType() => LowercaseToken(7);

    internal static string NewClaimValue() => LowercaseToken(11);

    internal static string NewRoleName() => LowercaseToken(8);

    internal static string NewGivenName() =>
        $"{(char)Random.Shared.Next('A', 'Z' + 1)}{LowercaseToken(7)}";

    internal static string NewPictureUrl() => $"https://{LowercaseToken(10)}.example/{LowercaseToken(8)}.jpg";

    internal static Guid NewUserId() => Guid.NewGuid();

    internal static int NewEntityId() => Random.Shared.Next(1, 100_000);

    internal static string NewRequestId() => LowercaseToken(12);

    internal static string NewClientIdentifier() => $"client-{LowercaseToken(10)}";

    internal static string NewClientName() => $"{LowercaseToken(5)} {LowercaseToken(7)}";

    internal static string NewApiResourceName() => $"api-{LowercaseToken(8)}";

    internal static string NewRecoveryCode() => Guid.NewGuid().ToString("N");

    internal static string NewAuthenticatorKey() => Guid.NewGuid().ToString("N").ToUpperInvariant();

    internal static string NewVerificationCode() =>
        Random.Shared.Next(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    internal static string NewTokenFromFirstHalfOfAlphabet(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('a', 'n')));

    internal static string NewTokenFromSecondHalfOfAlphabet(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('n', 'z' + 1)));

    internal static DateTime NewUtcDateTime() =>
        DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 100_000));

    internal static decimal NewScoreAtOrAboveDefaultThreshold() =>
        Identity.CAPTCHA.ReCAPTCHAOptions.DefaultScoreThreshold + (Random.Shared.Next(0, 6) / 10m);

    internal static decimal NewScoreBelowDefaultThreshold() =>
        Identity.CAPTCHA.ReCAPTCHAOptions.DefaultScoreThreshold - (Random.Shared.Next(1, 6) / 10m);

    internal static string NewBrowserUserAgent() => $"Mozilla/5.0 ({LowercaseToken(12)}) {LowercaseToken(8)}/1.0";

    internal static string NewSyntheticWalkerUserAgent() =>
        $"{NewBrowserUserAgent()} {Identity.Telemetry.Metrics.SyntheticUserAgentToken}/1.0";

    internal static string NewRecaptchaToken() => LowercaseToken(16);

    internal static string NewRecaptchaSecretKey() => LowercaseToken(24);

    internal static string NewRecaptchaSiteKey() => LowercaseToken(12);
}
