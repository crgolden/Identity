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
        // Arrange
        var passwords = Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength];

        passwords.Sample(password =>
        {
            // Arrange
            var user = new IdentityUser<Guid>();
            var hash = _hasher.HashPassword(user, password);

            // Act
            var result = _hasher.VerifyHashedPassword(user, hash, password);

            // Assert
            Assert.Equal(PasswordVerificationResult.Success, result);
        });
    }

    [Fact]
    public void HashPassword_SameInput_ProducesDifferentHashesEachTime()
    {
        // Arrange
        var passwords = Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength];

        passwords.Sample(password =>
        {
            // Arrange
            var user = new IdentityUser<Guid>();
            var firstHash = _hasher.HashPassword(user, password);

            // Act
            var secondHash = _hasher.HashPassword(user, password);

            // Assert
            Assert.NotEqual(firstHash, secondHash);
        });
    }

    [Fact]
    public void HashPassword_WrongPassword_NeverVerifies()
    {
        // Arrange
        var pairs = Gen.String[MinGeneratedPasswordLength, MaxGeneratedPasswordLength]
            .Select(p => (Password: p, Wrong: p + WrongPasswordSuffix));

        pairs.Sample(pair =>
        {
            // Arrange
            var user = new IdentityUser<Guid>();
            var hash = _hasher.HashPassword(user, pair.Password);

            // Act
            var result = _hasher.VerifyHashedPassword(user, hash, pair.Wrong);

            // Assert
            Assert.NotEqual(PasswordVerificationResult.Success, result);
        });
    }

    [Theory]
    [MemberData(nameof(NonAsciiPasswords))]
    public void HashPassword_UnicodePassword_RoundTrips(string password)
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, password);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, password);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }
}