namespace Identity.Tests.Unit.PropertyBased;

using System.Security.Cryptography;
using System.Text;
using CsCheck;
using Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class InputSanitizationTests
{
    [Fact]
    public void GravatarHash_IsAlwaysLowercase()
    {
        Gen.String[1, 200]
            .Sample(email =>
            {
                var hash = ComputeGravatarHash(email);
                Assert.Equal(hash, hash.ToLowerInvariant());
            });
    }

    [Fact]
    public void GravatarHash_IsAlways64HexChars()
    {
        Gen.String[1, 200]
            .Sample(email =>
            {
                var hash = ComputeGravatarHash(email);
                Assert.Equal(64, hash.Length);
                Assert.True(hash.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)));
            });
    }

    [Fact]
    public void GravatarHash_IsDeterministic()
    {
        Gen.String[1, 200]
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
        Gen.String[1, 50]
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
        var email = "  test@example.com  ";
        var trimmed = "test@example.com";
        Assert.Equal(ComputeGravatarHash(email), ComputeGravatarHash(trimmed));
    }

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com/path?query=1")]
    [InlineData("//evil.com")]
    [InlineData("//evil.com/path")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("https://evil.com/redirect?url=https://localhost")]
    [InlineData("\thttps://evil.com")]
    [InlineData(" https://evil.com")]
    public void ExternalUrl_IsNotLocalUrl(string url) =>
        Assert.False(IsLocalUrl(url), $"URL '{url}' was incorrectly classified as local.");

    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Manage")]
    [InlineData("/Account/Manage/ChangePassword")]
    [InlineData("~/Account/Login")]
    public void LocalUrl_IsLocalUrl(string url) =>
        Assert.True(IsLocalUrl(url), $"URL '{url}' should be local.");

    private static string ComputeGravatarHash(string identifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
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