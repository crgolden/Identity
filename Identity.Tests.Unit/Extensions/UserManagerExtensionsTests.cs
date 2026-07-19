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
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.GivenName, "Chris"),
            new Claim("picture", "https://example.com/pic.jpg")
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
        userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(
        [
            new Claim(ClaimTypes.Email, "already-set@example.com")
        ]);
        userManager
            .Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "from-google@example.com"), // type already present -> must NOT be added/overwritten
            new Claim(ClaimTypes.GivenName, "Chris") // new type -> should be added
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
        // Arrange: NameIdentifier is the same claim type UserClaimsPrincipalFactory uses for the
        // user's own ID — persisting the provider's subject identifier under it would create a
        // second, colliding claim of that type on every principal built for the user.
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
            new Claim(ClaimTypes.Email, "already-set@example.com")
        ]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "from-google@example.com")
        ]));

        // Act
        await userManager.Object.AddMissingClaimsAsync(user, principal);

        // Assert
        userManager.Verify(m => m.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }
}
