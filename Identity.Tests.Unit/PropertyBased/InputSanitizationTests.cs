namespace Identity.Tests.Unit.PropertyBased;

using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using CsCheck;
using Identity.Tests.Unit.Infrastructure;
using static Identity.Tests.Unit.PropertyBased.SanitizationFixtureConstants;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class InputSanitizationTests
{
    private static readonly string ProtocolRelativePrefix = Uri.SchemeDelimiter.TrimStart(SchemeSeparator);

    private static readonly int Sha256HexLength =
        Convert.ToHexString(new byte[SHA256.HashSizeInBytes]).Length;

    public static TheoryData<string> ExternalUrls()
    {
        var host = Generated.NewExternalHost();
        var absoluteUrl = Uri.UriSchemeHttps + Uri.SchemeDelimiter + host;
        return new TheoryData<string>
        {
            absoluteUrl,
            Uri.UriSchemeHttp + Uri.SchemeDelimiter + host + Generated.NewLocalPath() +
                QueryStringStart + Generated.NewPathSegment() + QueryStringAssignment + Generated.NewEntityId(),
            ProtocolRelativePrefix + host,
            ProtocolRelativePrefix + host + Generated.NewLocalPath(),
            JavaScriptScheme + SchemeSeparator + Generated.NewPathSegment(),
            DataScheme + SchemeSeparator + MediaTypeNames.Text.Html + DataUrlSeparator + Generated.NewPathSegment(),
            absoluteUrl + Generated.NewLocalPath() + QueryStringStart + Generated.NewPathSegment() +
                QueryStringAssignment + Uri.UriSchemeHttps + Uri.SchemeDelimiter + Generated.NewExternalHost(),
            TabCharacter + absoluteUrl,
            SpaceCharacter + absoluteUrl,
        };
    }

    public static TheoryData<string> LocalUrls() => new()
    {
        new string(PathSeparator, MinGeneratedInputLength),
        Generated.NewLocalPath(),
        Generated.NewLocalPath() + Generated.NewLocalPath(),
        Generated.NewLocalPath() + Generated.NewLocalPath() + Generated.NewLocalPath(),
        PageRoutes.ContentRoot + Generated.NewPathSegment(),
    };

    [Fact]
    public void GravatarHash_IsAlwaysLowercase()
    {
        // Arrange
        var emails = Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength];

        emails.Sample(email =>
        {
            // Act
            var hash = ComputeGravatarHash(email);

            // Assert
            Assert.Equal(hash, hash.ToLowerInvariant());
        });
    }

    [Fact]
    public void GravatarHash_IsAlways64HexChars()
    {
        // Arrange
        var emails = Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength];

        emails.Sample(email =>
        {
            // Act
            var hash = ComputeGravatarHash(email);

            // Assert
            Assert.Equal(Sha256HexLength, hash.Length);
            Assert.True(hash.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)));
        });
    }

    [Fact]
    public void GravatarHash_IsDeterministic()
    {
        // Arrange
        var emails = Gen.String[MinGeneratedInputLength, MaxGeneratedInputLength];

        emails.Sample(email =>
        {
            // Arrange
            var firstHash = ComputeGravatarHash(email);

            // Act
            var secondHash = ComputeGravatarHash(email);

            // Assert
            Assert.Equal(firstHash, secondHash);
        });
    }

    [Fact]
    public void GravatarHash_EmailNormalization_CaseInsensitive()
    {
        // Arrange
        var inputs = Gen.String[MinGeneratedInputLength, MaxNormalizedInputLength]
            .Select(s => s.Replace('\0', 'a').Trim())
            .Where(s => s.Length > 0);

        inputs.Sample(input =>
        {
            // Arrange
            var lowercaseHash = ComputeGravatarHash(input.ToLowerInvariant());

            // Act
            var uppercaseHash = ComputeGravatarHash(input.ToUpperInvariant());

            // Assert
            Assert.Equal(lowercaseHash, uppercaseHash);
        });
    }

    [Fact]
    public void GravatarHash_EmailWhitespaceTrimmed()
    {
        // Arrange
        var trimmed = Generated.NewEmailAddress();
        var paddedWithWhitespace = Generated.NewWhitespaceValue() + trimmed + Generated.NewWhitespaceValue();
        var trimmedHash = ComputeGravatarHash(trimmed);

        // Act
        var paddedHash = ComputeGravatarHash(paddedWithWhitespace);

        // Assert
        Assert.Equal(trimmedHash, paddedHash);
    }

    [Theory]
    [MemberData(nameof(ExternalUrls))]
    public void ExternalUrl_IsNotLocalUrl(string url)
    {
        // Act
        var isLocal = IsLocalUrl(url);

        // Assert
        Assert.False(isLocal);
    }

    [Theory]
    [MemberData(nameof(LocalUrls))]
    public void LocalUrl_IsLocalUrl(string url)
    {
        // Act
        var isLocal = IsLocalUrl(url);

        // Assert
        Assert.True(isLocal);
    }

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
