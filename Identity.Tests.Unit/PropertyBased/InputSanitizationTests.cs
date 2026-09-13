namespace Identity.Tests.Unit.PropertyBased;

using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using CsCheck;
using Infrastructure;
using static SanitizationFixtureConstants;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class InputSanitizationTests
{
    private static readonly string ProtocolRelativePrefix = Uri.SchemeDelimiter.TrimStart(SchemeSeparator);

    private static readonly int Sha256HexLength =
        Convert.ToHexString(new byte[SHA256.HashSizeInBytes]).Length;

    public static TheoryData<string> ExternalUrls()
    {
        var host = TestValues.NewExternalHost();
        var absoluteUrl = Uri.UriSchemeHttps + Uri.SchemeDelimiter + host;
        return new TheoryData<string>
        {
            absoluteUrl,
            Uri.UriSchemeHttp + Uri.SchemeDelimiter + host + TestValues.NewLocalPath() +
                QueryStringStart + TestValues.NewPathSegment() + QueryStringAssignment + TestValues.NewEntityId(),
            ProtocolRelativePrefix + host,
            ProtocolRelativePrefix + host + TestValues.NewLocalPath(),
            JavaScriptScheme + SchemeSeparator + TestValues.NewPathSegment(),
            DataScheme + SchemeSeparator + MediaTypeNames.Text.Html + DataUrlSeparator + TestValues.NewPathSegment(),
            absoluteUrl + TestValues.NewLocalPath() + QueryStringStart + TestValues.NewPathSegment() +
                QueryStringAssignment + Uri.UriSchemeHttps + Uri.SchemeDelimiter + TestValues.NewExternalHost(),
            TabCharacter + absoluteUrl,
            SpaceCharacter + absoluteUrl,
        };
    }

    public static TheoryData<string> LocalUrls() => new()
    {
        new string(PathSeparator, MinGeneratedInputLength),
        TestValues.NewLocalPath(),
        TestValues.NewLocalPath() + TestValues.NewLocalPath(),
        TestValues.NewLocalPath() + TestValues.NewLocalPath() + TestValues.NewLocalPath(),
        PageRoutes.ContentRoot + TestValues.NewPathSegment(),
    };

    [Fact]
    public void GravatarHash_IsAlwaysLowercase()
    {
        Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength]
            .Sample(email =>
            {
                var hash = ComputeGravatarHash(email);
                Assert.Equal(hash, hash.ToLowerInvariant());
            });
    }

    [Fact]
    public void GravatarHash_IsAlways64HexChars()
    {
        Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength]
            .Sample(email =>
            {
                var hash = ComputeGravatarHash(email);
                Assert.Equal(Sha256HexLength, hash.Length);
                Assert.True(hash.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)));
            });
    }

    [Fact]
    public void GravatarHash_IsDeterministic()
    {
        Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength]
            .Sample(email =>
            {
                var hash1 = ComputeGravatarHash(email);
                var hash2 = ComputeGravatarHash(email);
                Assert.Equal(hash1, hash2);
            });
    }

    [Fact]
    public void GravatarHash_EmailNormalization_CaseInsensitive()
    {
        Gen.String[MinGeneratedInputLength, MaxNormalizedInputLength]
            .Select(s => s.Replace('\0', 'a').Trim())
            .Where(s => s.Length > 0)
            .Sample(input =>
            {
                var lower = input.ToLowerInvariant();
                var upper = input.ToUpperInvariant();
                Assert.Equal(ComputeGravatarHash(lower), ComputeGravatarHash(upper));
            });
    }

    [Fact]
    public void GravatarHash_EmailWhitespaceTrimmed()
    {
        var trimmed = TestValues.NewEmailAddress();
        var paddedWithWhitespace = TestValues.NewWhitespaceValue() + trimmed + TestValues.NewWhitespaceValue();
        Assert.Equal(ComputeGravatarHash(paddedWithWhitespace), ComputeGravatarHash(trimmed));
    }

    [Theory]
    [MemberData(nameof(ExternalUrls))]
    public void ExternalUrl_IsNotLocalUrl(string url) => Assert.False(IsLocalUrl(url));

    [Theory]
    [MemberData(nameof(LocalUrls))]
    public void LocalUrl_IsLocalUrl(string url) => Assert.True(IsLocalUrl(url));

    private static string ComputeGravatarHash(string identifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url[0] == '/')
        {
            return url.Length == 1
                || (url[1] != '/' && url[1] != '\\');
        }

        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            return true;
        }

        return false;
    }
}