namespace Identity.Tests.Unit.Infrastructure;

internal static class TestValues
{
    internal static string LowercaseToken(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('a', 'z' + 1)));

    internal static string NewEmailAddress() => $"{LowercaseToken(8)}@{LowercaseToken(10)}.example";

    internal static string NewUserName() => LowercaseToken(9);

    internal static string NewTaggedEmailAddress() =>
        $"{LowercaseToken(6)}+{LowercaseToken(4)}@{LowercaseToken(8)}.example";

    internal static string NewNonAsciiEmailAddress() =>
        $"{LowercaseToken(5)}ä{LowercaseToken(4)}@{LowercaseToken(7)}.example";

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

    internal static string NewScopeName() => $"scope-{LowercaseToken(8)}";

    internal static string NewApiScopeName() => $"{NewApiResourceName()}.{LowercaseToken(5)}";

    internal static string NewDisplayName() => $"{LowercaseToken(6)} {LowercaseToken(8)}";

    internal static string NewDescription() => $"{LowercaseToken(7)} {LowercaseToken(9)} {LowercaseToken(6)}";

    internal static string NewSecretValue() => LowercaseToken(24);

    internal static string NewPropertyKey() => LowercaseToken(9);

    internal static string NewPropertyValue() => LowercaseToken(13);

    internal static string NewGrantType() => LowercaseToken(11);

    internal static string NewExternalHost() => $"{LowercaseToken(10)}.example";

    internal static string NewOrigin() => Uri.UriSchemeHttps + Uri.SchemeDelimiter + NewExternalHost();

    internal static string NewCallbackUrl() => $"{NewOrigin()}/{LowercaseToken(7)}";

    internal static string NewSchemeName() => LowercaseToken(10);

    internal static string NewEntityIdUrn() => $"urn:{LowercaseToken(6)}:{LowercaseToken(8)}";

    internal static string NewSessionKey() => Guid.NewGuid().ToString("N");

    internal static string NewDeviceCode() => Guid.NewGuid().ToString("N");

    internal static string NewSubjectId() => Guid.NewGuid().ToString();

    internal static string NewNumericSubjectId() =>
        string.Concat(Enumerable.Range(0, 21).Select(_ => (char)Random.Shared.Next('0', '9' + 1)));

    internal static string NewLocalPath() => '/' + LowercaseToken(8);

    internal static string NewEmailConfirmationToken() => Guid.NewGuid().ToString("N");

    internal static string NewModelStateKey() => LowercaseToken(7);

    internal static int NewRecoveryCodeCount() => Random.Shared.Next(1, 11);

    internal static string NewPhoneNumber() =>
        $"+1{Random.Shared.Next(2_000_000, 10_000_000)}{Random.Shared.Next(100, 1000)}";

    internal static string NewValidationMessage() => $"{LowercaseToken(6)} {LowercaseToken(9)}";

    internal static string NewFirstAlphabeticalName() => NewTokenFromFirstHalfOfAlphabet(9);

    internal static string NewLastAlphabeticalName() => NewTokenFromSecondHalfOfAlphabet(9);

    internal static string NewActivitySourceName() => $"{LowercaseToken(6)}.{LowercaseToken(6)}";

    internal static string NewActivityName() => $"{LowercaseToken(5)}-{LowercaseToken(9)}";

    internal static string NewKeyId() => Guid.NewGuid().ToString("N");

    internal static string NewSigningAlgorithmName() => $"RS{Random.Shared.Next(256, 513)}";

    internal static string NewLogoutId() => Guid.NewGuid().ToString("N");

    internal static string NewReferenceValueHash() => Guid.NewGuid().ToString("N");

    internal static byte[] NewByteSequence(int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }

    internal static byte[] NewIPv4AddressBytes() => NewByteSequence(4);

    internal static byte[] NewCredentialIdBytes() => NewByteSequence(16);

    internal static byte[] NewPublicKeyBytes() => NewByteSequence(32);

    internal static byte[] NewAttestationObjectBytes() => NewByteSequence(24);

    internal static byte[] NewClientDataJsonBytes() => NewByteSequence(26);

    internal static string NewHostLabel() => LowercaseToken(8);

    internal static string NewTenantName() => LowercaseToken(8);

    internal static string NewButtonValue() => LowercaseToken(6);

    internal static string NewPasskeyAction() => LowercaseToken(6);

    internal static string NewAttributeName() => LowercaseToken(7);

    internal static string NewAttributeValue() => LowercaseToken(10);

    internal static string NewButtonLabel() => LowercaseToken(9);

    internal static string NewPolicyDirectiveSource() => LowercaseToken(9);

    internal static string NewOverlongDisplayName() => LowercaseToken(80);

    internal static string NewRecoveryCode() => Guid.NewGuid().ToString("N");

    internal static string NewAuthenticatorKey() => Guid.NewGuid().ToString("N").ToUpperInvariant();

    internal static string NewVerificationCode() =>
        Random.Shared.Next(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    internal static string NewTokenFromFirstHalfOfAlphabet(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('a', 'n')));

    internal static string NewTokenFromSecondHalfOfAlphabet(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next('n', 'z' + 1)));

    internal static string NewPageName() => NewTokenFromFirstHalfOfAlphabet(8);

    internal static string NewDifferentPageName() => NewTokenFromSecondHalfOfAlphabet(8);

    internal static string NewPathSegment() => LowercaseToken(6);

    internal static string NewPunctuatedPageName() =>
        LowercaseToken(6) + string.Concat(Enumerable.Range(0, 4).Select(_ => (char)Random.Shared.Next('!', '-')));

    internal static string NewOverlongPageName() =>
        new((char)Random.Shared.Next('a', 'z' + 1), Random.Shared.Next(600, 1_200));

    internal static string NewWhitespaceValue() => new(' ', Random.Shared.Next(1, 6));

    internal static string WithFormattingSeparators(string value)
    {
        var firstBreak = Random.Shared.Next(1, value.Length);
        var secondBreak = Random.Shared.Next(firstBreak, value.Length);
        return string.Concat(value[..firstBreak], ' ', value[firstBreak..secondBreak], '-', value[secondBreak..]);
    }

    internal static string WithEmbeddedWhitespace(string value) =>
        value.Insert(Random.Shared.Next(1, value.Length), NewWhitespaceValue());

    internal static string NewTokenFromCodePointRange(int firstCodePoint, int lastCodePoint) =>
        string.Concat(Enumerable
            .Range(0, Random.Shared.Next(6, 12))
            .Select(_ => char.ConvertFromUtf32(Random.Shared.Next(firstCodePoint, lastCodePoint + 1))));

    internal static string NewOverlongValue() =>
        new((char)Random.Shared.Next('a', 'z' + 1), Random.Shared.Next(5_000, 10_000));

    internal static string NewControlAndSymbolValue() =>
        NewPunctuatedPageName() + '\0' + '\n' + '\t' + '☃';

    internal static DateTimeOffset NewUtcInstant() =>
        DateTimeOffset.UtcNow.AddMinutes(-Random.Shared.Next(1, 100_000));

    internal static DateTime NewUtcDateTime() => NewUtcInstant().UtcDateTime;

    internal static DateTimeOffset NewUtcInstantBefore(DateTimeOffset instant) =>
        instant.AddMinutes(-Random.Shared.Next(1, 100_000));

    internal static decimal NewScoreAtOrAboveDefaultThreshold() =>
        Identity.CAPTCHA.ReCAPTCHAOptions.DefaultScoreThreshold + (Random.Shared.Next(0, 6) / 10m);

    internal static decimal NewScoreBelowDefaultThreshold() =>
        Identity.CAPTCHA.ReCAPTCHAOptions.DefaultScoreThreshold - (Random.Shared.Next(1, 6) / 10m);

    internal static string NewBrowserUserAgent() => $"Mozilla/5.0 ({LowercaseToken(12)}) {LowercaseToken(8)}/1.0";

    internal static string NewSyntheticWalkerUserAgent() =>
        $"{NewBrowserUserAgent()} {Identity.Telemetry.Metrics.SyntheticUserAgentToken}/1.0";

    internal static string NewPasskeyOptionsJson() => $"{{\"challenge\":\"{Guid.NewGuid():N}\"}}";

    internal static string NewPasskeyCredentialJson() =>
        $"{{\"id\":\"{LowercaseToken(16)}\",\"type\":\"public-key\"}}";

    internal static string NewRecaptchaToken() => LowercaseToken(16);

    internal static string NewRecaptchaSecretKey() => LowercaseToken(24);

    internal static string NewRecaptchaSiteKey() => LowercaseToken(12);
}
