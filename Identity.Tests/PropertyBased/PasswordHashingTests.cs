namespace Identity.Tests.PropertyBased;

using CsCheck;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

/// <summary>
/// Property-based tests for ASP.NET Core Identity's PasswordHasher.
/// Verifies round-trip correctness and cross-instance consistency across
/// a large random sample of inputs.
/// </summary>
[Trait("Category", "Unit")]
public sealed class PasswordHashingTests
{
    // Use 1 PBKDF2 iteration — these tests verify API contract (round-trip, uniqueness,
    // wrong-password rejection), not the security of the iteration count. Default 600k
    // iterations × ~100 CsCheck samples = minutes of CPU time per test method.
    private readonly PasswordHasher<IdentityUser<Guid>> _hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 1 }));

    [Fact]
    public void HashPassword_ThenVerify_AlwaysSucceeds()
    {
        // Generate non-empty strings up to 72 chars, a realistic UX upper bound.
        Gen.String[1, 72]
            .Sample(password =>
            {
                var user = new IdentityUser<Guid>();
                var hash = _hasher.HashPassword(user, password);
                var result = _hasher.VerifyHashedPassword(user, hash, password);
                Assert.Equal(PasswordVerificationResult.Success, result);
            });
    }

    [Fact]
    public void HashPassword_SameInput_ProducesDifferentHashesEachTime()
    {
        // PBKDF2 uses a random salt per invocation — the same password must
        // never produce the same hash twice.
        Gen.String[1, 72]
            .Sample(password =>
            {
                var user = new IdentityUser<Guid>();
                var hash1 = _hasher.HashPassword(user, password);
                var hash2 = _hasher.HashPassword(user, password);
                Assert.NotEqual(hash1, hash2);
            });
    }

    [Fact]
    public void HashPassword_WrongPassword_NeverVerifies()
    {
        Gen.String[1, 72]
            .Select(p => (Password: p, Wrong: p + "X"))
            .Sample(pair =>
            {
                var user = new IdentityUser<Guid>();
                var hash = _hasher.HashPassword(user, pair.Password);
                var result = _hasher.VerifyHashedPassword(user, hash, pair.Wrong);
                Assert.NotEqual(PasswordVerificationResult.Success, result);
            });
    }

    [Fact]
    public void HashPassword_UnicodePasswords_RoundTrip()
    {
        var unicodePasswords = new[]
        {
            "Ünïcödé@123!",
            "日本語パスワード1!",
            "العربية123!",
            "Ελληνικά123!",
            "한국어비밀번호1!",
            "Ру́сский123!",
            "中文密码123!",
            "🔐password1!"
        };

        foreach (var password in unicodePasswords)
        {
            var user = new IdentityUser<Guid>();
            var hash = _hasher.HashPassword(user, password);
            var result = _hasher.VerifyHashedPassword(user, hash, password);
            Assert.Equal(PasswordVerificationResult.Success, result);
        }
    }
}
