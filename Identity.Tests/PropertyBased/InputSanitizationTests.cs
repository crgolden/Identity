namespace Identity.Tests.PropertyBased;

using System.Security.Cryptography;
using System.Text;
using CsCheck;

/// <summary>
/// Property-based tests for input handling: Gravatar hash computation,
/// URL safety classification, and email normalization.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InputSanitizationTests
{
    // Mirrors the implementation in GravatarService to test it as a pure function.
    private static string ComputeGravatarHash(string identifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Fact]
    public void GravatarHash_IsAlwaysLowercase()
    {
        Gen.String[1..200]
            .Sample(email =>
            {
                var hash = ComputeGravatarHash(email);
                Assert.Equal(hash, hash.ToLowerInvariant());
            });
    }

    [Fact]
    public void GravatarHash_IsAlways64HexChars()
    {
        Gen.String[1..200]
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
        Gen.String[1..200]
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
        // The Gravatar spec mandates trimming and lowercasing before hashing.
        // Verify the hash is the same regardless of the email's case.
        Gen.String[1..50]
            .Select(s => s.Replace('\0', 'a').Trim()) // avoid control chars
            .Where(s => s.Length > 0)
            .Sample(base64 =>
            {
                var lower = base64.ToLowerInvariant();
                var upper = base64.ToUpperInvariant();
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

    [Fact]
    public void KnownExternalUrls_AreNotLocalUrls()
    {
        // Known attack vectors for open redirect — none of these should be
        // treated as local by LocalRedirectResult/Url.IsLocalUrl().
        var externalUrls = new[]
        {
            "https://evil.com",
            "http://evil.com/path?query=1",
            "//evil.com",
            "//evil.com/path",
            "javascript:alert(1)",
            "data:text/html,<script>alert(1)</script>",
            "https://evil.com/redirect?url=https://localhost",
            "\thttps://evil.com",
            " https://evil.com",
        };

        foreach (var url in externalUrls)
        {
            Assert.False(
                IsLocalUrl(url),
                $"URL '{url}' was incorrectly classified as local.");
        }
    }

    [Fact]
    public void KnownLocalUrls_AreLocalUrls()
    {
        var localUrls = new[]
        {
            "/",
            "/Account/Login",
            "/Account/Manage",
            "/Account/Manage/ChangePassword",
            "~/Account/Login"
        };

        foreach (var url in localUrls)
        {
            Assert.True(IsLocalUrl(url), $"URL '{url}' should be local.");
        }
    }

    /// <summary>
    /// Mirrors the ASP.NET Core UrlHelper.IsLocalUrl logic used by LocalRedirect.
    /// </summary>
    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Allows "/" or "~/" but not "//" or "/ "
        if (url[0] == '/')
        {
            return url.Length == 1
                || (url[1] != '/' && url[1] != '\\');
        }

        // Allows "~/"
        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            return true;
        }

        return false;
    }
}
