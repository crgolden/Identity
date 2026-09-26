namespace Identity.Tests.Unit.PropertyBased;

using CsCheck;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using static Identity.Tests.Unit.PropertyBased.PasswordFixtureConstants;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class PasswordHashingTests
{
    private readonly PasswordHasher<IdentityUser<Guid>> _hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 1 }));

    public static TheoryData<string> NonAsciiPasswords() => new()
    {
        Generated.NewTokenFromCodePointRange(LatinSupplementFirstCodePoint, LatinSupplementLastCodePoint),
        Generated.NewTokenFromCodePointRange(GreekFirstCodePoint, GreekLastCodePoint),
        Generated.NewTokenFromCodePointRange(CyrillicFirstCodePoint, CyrillicLastCodePoint),
        Generated.NewTokenFromCodePointRange(ArabicFirstCodePoint, ArabicLastCodePoint),
        Generated.NewTokenFromCodePointRange(HiraganaFirstCodePoint, KatakanaLastCodePoint),
        Generated.NewTokenFromCodePointRange(CjkFirstCodePoint, CjkLastCodePoint),
        Generated.NewTokenFromCodePointRange(HangulFirstCodePoint, HangulLastCodePoint),
        Generated.NewTokenFromCodePointRange(EmojiFirstCodePoint, EmojiLastCodePoint),
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
