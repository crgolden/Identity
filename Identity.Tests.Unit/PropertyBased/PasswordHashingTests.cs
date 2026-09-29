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

    [Fact]
    public void HashPassword_LatinSupplementPassword_RoundTrips()
    {
        // Arrange
        var latinSupplementPassword = Generated.NewTokenFromCodePointRange(LatinSupplementFirstCodePoint, LatinSupplementLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, latinSupplementPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, latinSupplementPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_GreekPassword_RoundTrips()
    {
        // Arrange
        var greekPassword = Generated.NewTokenFromCodePointRange(GreekFirstCodePoint, GreekLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, greekPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, greekPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_CyrillicPassword_RoundTrips()
    {
        // Arrange
        var cyrillicPassword = Generated.NewTokenFromCodePointRange(CyrillicFirstCodePoint, CyrillicLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, cyrillicPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, cyrillicPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_ArabicPassword_RoundTrips()
    {
        // Arrange
        var arabicPassword = Generated.NewTokenFromCodePointRange(ArabicFirstCodePoint, ArabicLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, arabicPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, arabicPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_KanaPassword_RoundTrips()
    {
        // Arrange
        var kanaPassword = Generated.NewTokenFromCodePointRange(HiraganaFirstCodePoint, KatakanaLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, kanaPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, kanaPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_CjkPassword_RoundTrips()
    {
        // Arrange
        var cjkPassword = Generated.NewTokenFromCodePointRange(CjkFirstCodePoint, CjkLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, cjkPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, cjkPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_HangulPassword_RoundTrips()
    {
        // Arrange
        var hangulPassword = Generated.NewTokenFromCodePointRange(HangulFirstCodePoint, HangulLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, hangulPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, hangulPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void HashPassword_EmojiPassword_RoundTrips()
    {
        // Arrange
        var emojiPassword = Generated.NewTokenFromCodePointRange(EmojiFirstCodePoint, EmojiLastCodePoint);
        var user = new IdentityUser<Guid>();
        var hash = _hasher.HashPassword(user, emojiPassword);

        // Act
        var result = _hasher.VerifyHashedPassword(user, hash, emojiPassword);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, result);
    }
}
