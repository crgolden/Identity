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

    [Fact]
    public void IsLocalUrl_HttpsAbsoluteUrl_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var httpsUrl = Uri.UriSchemeHttps + Uri.SchemeDelimiter + externalHost;

        // Act
        var isLocal = IsLocalUrl(httpsUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_HttpAbsoluteUrlWithPathAndQuery_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var path = Generated.NewLocalPath();
        var queryKey = Generated.NewPathSegment();
        var queryValue = Generated.NewEntityId();
        var httpUrl = Uri.UriSchemeHttp + Uri.SchemeDelimiter + externalHost + path +
            QueryStringStart + queryKey + QueryStringAssignment + queryValue;

        // Act
        var isLocal = IsLocalUrl(httpUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_ProtocolRelativeUrl_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var protocolRelativeUrl = ProtocolRelativePrefix + externalHost;

        // Act
        var isLocal = IsLocalUrl(protocolRelativeUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_ProtocolRelativeUrlWithPath_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var path = Generated.NewLocalPath();
        var protocolRelativeUrl = ProtocolRelativePrefix + externalHost + path;

        // Act
        var isLocal = IsLocalUrl(protocolRelativeUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_JavaScriptUrl_ReturnsFalse()
    {
        // Arrange
        var script = Generated.NewPathSegment();
        var javaScriptUrl = JavaScriptScheme + SchemeSeparator + script;

        // Act
        var isLocal = IsLocalUrl(javaScriptUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_DataUrl_ReturnsFalse()
    {
        // Arrange
        var payload = Generated.NewPathSegment();
        var dataUrl = DataScheme + SchemeSeparator + MediaTypeNames.Text.Html + DataUrlSeparator + payload;

        // Act
        var isLocal = IsLocalUrl(dataUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_AbsoluteUrlCarryingAnotherAbsoluteUrlInItsQuery_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var path = Generated.NewLocalPath();
        var queryKey = Generated.NewPathSegment();
        var redirectHost = Generated.NewExternalHost();
        var urlWithRedirectQuery = Uri.UriSchemeHttps + Uri.SchemeDelimiter + externalHost + path + QueryStringStart + queryKey +
            QueryStringAssignment + Uri.UriSchemeHttps + Uri.SchemeDelimiter + redirectHost;

        // Act
        var isLocal = IsLocalUrl(urlWithRedirectQuery);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_TabPrefixedAbsoluteUrl_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var tabPrefixedUrl = TabCharacter + Uri.UriSchemeHttps + Uri.SchemeDelimiter + externalHost;

        // Act
        var isLocal = IsLocalUrl(tabPrefixedUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_SpacePrefixedAbsoluteUrl_ReturnsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var spacePrefixedUrl = SpaceCharacter + Uri.UriSchemeHttps + Uri.SchemeDelimiter + externalHost;

        // Act
        var isLocal = IsLocalUrl(spacePrefixedUrl);

        // Assert
        Assert.False(isLocal);
    }

    [Fact]
    public void IsLocalUrl_RootPath_ReturnsTrue()
    {
        // Arrange
        var rootPath = new string(PathSeparator, MinGeneratedInputLength);

        // Act
        var isLocal = IsLocalUrl(rootPath);

        // Assert
        Assert.True(isLocal);
    }

    [Fact]
    public void IsLocalUrl_SingleSegmentPath_ReturnsTrue()
    {
        // Arrange
        var singleSegmentPath = Generated.NewLocalPath();

        // Act
        var isLocal = IsLocalUrl(singleSegmentPath);

        // Assert
        Assert.True(isLocal);
    }

    [Fact]
    public void IsLocalUrl_TwoSegmentPath_ReturnsTrue()
    {
        // Arrange
        var firstSegmentPath = Generated.NewLocalPath();
        var secondSegmentPath = Generated.NewLocalPath();
        var twoSegmentPath = firstSegmentPath + secondSegmentPath;

        // Act
        var isLocal = IsLocalUrl(twoSegmentPath);

        // Assert
        Assert.True(isLocal);
    }

    [Fact]
    public void IsLocalUrl_ThreeSegmentPath_ReturnsTrue()
    {
        // Arrange
        var firstSegmentPath = Generated.NewLocalPath();
        var secondSegmentPath = Generated.NewLocalPath();
        var thirdSegmentPath = Generated.NewLocalPath();
        var threeSegmentPath = firstSegmentPath + secondSegmentPath + thirdSegmentPath;

        // Act
        var isLocal = IsLocalUrl(threeSegmentPath);

        // Assert
        Assert.True(isLocal);
    }

    [Fact]
    public void IsLocalUrl_ContentRootRelativePath_ReturnsTrue()
    {
        // Arrange
        var pageSegment = Generated.NewPathSegment();
        var contentRootRelativePath = PageRoutes.ContentRoot + pageSegment;

        // Act
        var isLocal = IsLocalUrl(contentRootRelativePath);

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
