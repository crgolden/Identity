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
}
