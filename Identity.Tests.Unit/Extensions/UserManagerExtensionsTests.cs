namespace Identity.Tests.Unit.Extensions;

using System.Security.Claims;
using Identity.Extensions;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class UserManagerExtensionsTests
{
    [Fact]
    public async Task AddMissingClaimsAsync_UserHasNoClaims_AddsEveryPrincipalClaim()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        Claim[] principalClaims =
        [
            new Claim(ClaimTypes.Email, Generated.NewEmailAddress()),
            new Claim(ClaimTypes.GivenName, Generated.NewGivenName()),
            new Claim(Identity.Avatar.AvatarProfileService.PictureClaimType, Generated.NewPictureAddress())
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(principalClaims));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(
                user,
                It.Is<IEnumerable<Claim>>(claims => claims.Count() == principalClaims.Length)),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_UserAlreadyHasSomeClaimTypes_OnlyAddsMissingTypes()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManager = MockHelpers.MockUserManager();
        var emailAlreadyOnTheUser = Generated.NewEmailAddress();
        var emailOfferedByTheProvider = Generated.NewEmailAddress();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(
        [
            new Claim(ClaimTypes.Email, emailAlreadyOnTheUser)
        ]);
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, emailOfferedByTheProvider),
            new Claim(ClaimTypes.GivenName, Generated.NewGivenName())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(
                user,
                It.Is<IEnumerable<Claim>>(claims =>
                    claims.Select(c => c.Type).SequenceEqual(new[] { ClaimTypes.GivenName }))),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_NameIdentifierClaim_IsNeverPersisted()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Generated.NewNumericSubjectId()),
            new Claim(ClaimTypes.GivenName, Generated.NewGivenName())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(
                user,
                It.Is<IEnumerable<Claim>>(claims =>
                    claims.Select(c => c.Type).SequenceEqual(new[] { ClaimTypes.GivenName }))),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_OnlyNameIdentifierClaimPresent_DoesNotCallAddClaimsAsync()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Generated.NewNumericSubjectId())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(m => m.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_UserAlreadyHasAllClaimTypes_DoesNotCallAddClaimsAsync()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(
        [
            new Claim(ClaimTypes.Email, Generated.NewEmailAddress())
        ]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, Generated.NewEmailAddress())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(m => m.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }
}
