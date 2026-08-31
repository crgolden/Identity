namespace Identity.Tests.Unit.Extensions;

using System.Security.Claims;
using Identity.Extensions;
using Infrastructure;
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
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, TestValues.NewEmailAddress()),
            new Claim(ClaimTypes.GivenName, TestValues.NewGivenName()),
            new Claim("picture", TestValues.NewPictureUrl())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(user, It.Is<IEnumerable<Claim>>(claims => claims.Count() == 3)),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_UserAlreadyHasSomeClaimTypes_OnlyAddsMissingTypes()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userManager = MockHelpers.MockUserManager();
        var emailAlreadyOnTheUser = TestValues.NewEmailAddress();
        var emailOfferedByTheProvider = TestValues.NewEmailAddress();
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
            new Claim(ClaimTypes.GivenName, TestValues.NewGivenName())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(
                user,
                It.Is<IEnumerable<Claim>>(claims =>
                    claims.Count() == 1 &&
                    claims.Single().Type == ClaimTypes.GivenName)),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_NameIdentifierClaim_IsNeverPersisted()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "115104222051319378434"),
            new Claim(ClaimTypes.GivenName, "Chris")
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(
            m => m.AddClaimsAsync(
                user,
                It.Is<IEnumerable<Claim>>(claims =>
                    claims.Count() == 1 &&
                    claims.Single().Type == ClaimTypes.GivenName)),
            Times.Once);
    }

    [Fact]
    public async Task AddMissingClaimsAsync_OnlyNameIdentifierClaimPresent_DoesNotCallAddClaimsAsync()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "115104222051319378434")
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
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(
        [
            new Claim(ClaimTypes.Email, TestValues.NewEmailAddress())
        ]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, TestValues.NewEmailAddress())
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(m => m.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }
}