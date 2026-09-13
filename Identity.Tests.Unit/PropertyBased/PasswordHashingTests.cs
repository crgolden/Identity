namespace Identity.Tests.Unit.PropertyBased;

using CsCheck;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using static PasswordFixtureConstants;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class PasswordHashingTests
{
    private readonly PasswordHasher<IdentityUser<Guid>> _hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 1 }));

    public static TheoryData<string> NonAsciiPasswords() => new()
    {
        TestValues.NewTokenFromCodePointRange(LatinSupplementFirstCodePoint, LatinSupplementLastCodePoint),
        TestValues.NewTokenFromCodePointRange(GreekFirstCodePoint, GreekLastCodePoint),
        TestValues.NewTokenFromCodePointRange(CyrillicFirstCodePoint, CyrillicLastCodePoint),
        TestValues.NewTokenFromCodePointRange(ArabicFirstCodePoint, ArabicLastCodePoint),
        TestValues.NewTokenFromCodePointRange(HiraganaFirstCodePoint, KatakanaLastCodePoint),
        TestValues.NewTokenFromCodePointRange(CjkFirstCodePoint, CjkLastCodePoint),
        TestValues.NewTokenFromCodePointRange(HangulFirstCodePoint, HangulLastCodePoint),
        TestValues.NewTokenFromCodePointRange(EmojiFirstCodePoint, EmojiLastCodePoint),
    };

    [Fact]
    public void HashPassword_ThenVerify_AlwaysSucceeds()
    {
        Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength]
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
        Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength]
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
        Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength]
            .Select(p => (Password: p, Wrong: p + WrongPasswordSuffix))
            .Sample(pair =>
            {
                var user = new IdentityUser<Guid>();
                var hash = _hasher.HashPassword(user, pair.Password);
                var result = _hasher.VerifyHashedPassword(user, hash, pair.Wrong);
                Assert.NotEqual(PasswordVerificationResult.Success, result);
            });
    }

    [Theory]
    [MemberData(nameof(NonAsciiPasswords))]
    public void HashPassword_UnicodePassword_RoundTrips(string password)
    {
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, password);
        var result = _hasher.VerifyHashedPassword(user, hash, password);
        Assert.Equal(PasswordVerificationResult.Success, result);
    }
}