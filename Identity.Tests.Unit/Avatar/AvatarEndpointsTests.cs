namespace Identity.Tests.Unit.Avatar;

using System.Security.Claims;
using Identity.Avatar;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AvatarEndpointsTests
{
    [Fact]
    public async Task GetAvatarAsync_RedirectsToAStoredHttpsPictureClaim()
    {
        // Arrange
        var googlePhotoUrl = $"https://lh3.googleusercontent.com/{Guid.NewGuid()}";
        var user = UserWithEmail($"{Guid.NewGuid()}@example.com");
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService.Setup(x => x.IsOwnComputedUrl(googlePhotoUrl)).Returns(false);
        var userManager = UserManagerFor(user, [new Claim(AvatarProfileService.PictureClaimType, googlePhotoUrl)]);

        // Act
        var result = await AvatarEndpoints.GetAvatarAsync(
            user.Id.ToString(),
            userManager.Object,
            avatarService.Object,
            TestContext.Current.CancellationToken);

        // Assert
        var redirect = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal(googlePhotoUrl, redirect.Url);
        avatarService.Verify(
            x => x.GetAvatarUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvatarAsync_IgnoresAStoredClaimWhoseSchemeIsNotHttpsAndComputesInstead()
    {
        // Arrange
        var scriptUrl = $"javascript:alert('{Guid.NewGuid()}')";
        var computed = new Uri($"https://gravatar.com/avatar/{Guid.NewGuid():N}?s=2048&d=identicon");
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = UserWithEmail(emailAddress);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService.Setup(x => x.IsOwnComputedUrl(scriptUrl)).Returns(false);
        avatarService
            .Setup(x => x.GetAvatarUrlAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(computed);
        var userManager = UserManagerFor(user, [new Claim(AvatarProfileService.PictureClaimType, scriptUrl)]);

        // Act
        var result = await AvatarEndpoints.GetAvatarAsync(
            user.Id.ToString(),
            userManager.Object,
            avatarService.Object,
            TestContext.Current.CancellationToken);

        // Assert
        var redirect = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal(computed.AbsoluteUri, redirect.Url);
    }

    [Fact]
    public async Task GetAvatarAsync_FallsBackToTheComputedUrlWhenNoClaimIsStored()
    {
        // Arrange
        var computed = new Uri($"https://gravatar.com/avatar/{Guid.NewGuid():N}?s=2048&d=identicon");
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = UserWithEmail(emailAddress);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService
            .Setup(x => x.GetAvatarUrlAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(computed);
        var userManager = UserManagerFor(user, []);

        // Act
        var result = await AvatarEndpoints.GetAvatarAsync(
            user.Id.ToString(),
            userManager.Object,
            avatarService.Object,
            TestContext.Current.CancellationToken);

        // Assert
        var redirect = Assert.IsType<RedirectHttpResult>(result);
        Assert.Equal(computed.AbsoluteUri, redirect.Url);
    }

    [Fact]
    public async Task GetAvatarAsync_ReturnsNotFoundForASubWithNoUser()
    {
        // Arrange
        var unknownSub = Guid.NewGuid().ToString();
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(x => x.FindByIdAsync(unknownSub)).ReturnsAsync((IdentityUser<Guid>?)null);

        // Act
        var result = await AvatarEndpoints.GetAvatarAsync(
            unknownSub,
            userManager.Object,
            avatarService.Object,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<NotFound>(result);
    }

    private static IdentityUser<Guid> UserWithEmail(string emailAddress)
    {
        var userId = Guid.NewGuid();
        return new IdentityUser<Guid> { Id = userId, Email = emailAddress, UserName = emailAddress };
    }

    private static Mock<UserManager<IdentityUser<Guid>>> UserManagerFor(
        IdentityUser<Guid> user,
        IList<Claim> storedClaims)
    {
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(storedClaims);
        userManager.Setup(x => x.GetEmailAsync(user)).ReturnsAsync(user.Email);
        userManager.Setup(x => x.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
        return userManager;
    }
}
