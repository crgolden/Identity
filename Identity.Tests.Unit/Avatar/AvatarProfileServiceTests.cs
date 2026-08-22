namespace Identity.Tests.Unit.Avatar;

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Identity.Avatar;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AvatarProfileServiceTests
{
    private const string EmailClaimType = "email";
    private const string SubjectClaimType = "sub";
    private const string AuthenticationType = "test";

    [Fact]
    public async Task GetProfileDataAsync_PrefersAStoredPictureClaimOverTheComputedGravatarUrl()
    {
        // Arrange
        var googlePhotoUrl = $"https://lh3.googleusercontent.com/{Guid.NewGuid()}";
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = emailAddress, UserName = emailAddress };
        var context = ProfileContextFor(user);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService.Setup(x => x.IsOwnComputedUrl(googlePhotoUrl)).Returns(false);
        var service = ServiceFor(
            user,
            avatarService.Object,
            storedClaims: [new Claim(AvatarProfileService.PictureClaimType, googlePhotoUrl)]);

        // Act
        await service.GetProfileDataAsync(context, TestContext.Current.CancellationToken);

        // Assert
        var picture = Assert.Single(
            context.IssuedClaims,
            x => string.Equals(x.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal));
        Assert.Equal(googlePhotoUrl, picture.Value);
        avatarService.Verify(
            x => x.GetAvatarUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProfileDataAsync_FallsBackToTheComputedGravatarUrlWhenNoPictureClaimIsStored()
    {
        // Arrange
        var gravatarUrl = new Uri($"https://gravatar.com/avatar/{Guid.NewGuid():N}");
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = emailAddress, UserName = emailAddress };
        var context = ProfileContextFor(user);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService
            .Setup(x => x.GetAvatarUrlAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(gravatarUrl);
        var service = ServiceFor(user, avatarService.Object, storedClaims: []);

        // Act
        await service.GetProfileDataAsync(context, TestContext.Current.CancellationToken);

        // Assert
        var picture = Assert.Single(
            context.IssuedClaims,
            x => string.Equals(x.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal));
        Assert.Equal(gravatarUrl.ToString(), picture.Value);
    }

    [Fact]
    public async Task GetProfileDataAsync_RecomputesOverALegacyGravatarClaimLeftByThePictureClaimWorker()
    {
        // Arrange
        var legacyUrl = $"https://0.gravatar.com/avatar/{Guid.NewGuid():N}";
        var recomputed = new Uri($"https://gravatar.com/avatar/{Guid.NewGuid():N}?s=2048&d=identicon");
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = emailAddress, UserName = emailAddress };
        var context = ProfileContextFor(user);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        avatarService.Setup(x => x.IsOwnComputedUrl(legacyUrl)).Returns(true);
        avatarService
            .Setup(x => x.GetAvatarUrlAsync(emailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recomputed);
        var service = ServiceFor(
            user,
            avatarService.Object,
            storedClaims: [new Claim(AvatarProfileService.PictureClaimType, legacyUrl)]);

        // Act
        await service.GetProfileDataAsync(context, TestContext.Current.CancellationToken);

        // Assert
        var picture = Assert.Single(
            context.IssuedClaims,
            x => string.Equals(x.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal));
        Assert.Equal(recomputed.AbsoluteUri, picture.Value);
    }

    [Fact]
    public async Task GetProfileDataAsync_AddsNoPictureWhenTheClientDidNotRequestIt()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = emailAddress, UserName = emailAddress };
        var context = ProfileContextFor(user, [EmailClaimType]);
        var avatarService = new Mock<IAvatarService>(MockBehavior.Strict);
        var service = ServiceFor(user, avatarService.Object, storedClaims: []);

        // Act
        await service.GetProfileDataAsync(context, TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(
            context.IssuedClaims,
            x => string.Equals(x.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal));
        avatarService.Verify(
            x => x.GetAvatarUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ProfileDataRequestContext ProfileContextFor(IdentityUser<Guid> user)
        => ProfileContextFor(user, [AvatarProfileService.PictureClaimType, EmailClaimType]);

    private static ProfileDataRequestContext ProfileContextFor(
        IdentityUser<Guid> user,
        string[] requestedClaimTypes)
    {
        var subject = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(SubjectClaimType, user.Id.ToString())], AuthenticationType));
        return new ProfileDataRequestContext(
            subject,
            new Client(),
            AuthenticationType,
            requestedClaimTypes);
    }

    private static AvatarProfileService ServiceFor(
        IdentityUser<Guid> user,
        IAvatarService avatarService,
        IList<Claim> storedClaims)
    {
        var userManager = MockHelpers.MockUserManager();
        userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(storedClaims);
        userManager.Setup(x => x.GetEmailAsync(user)).ReturnsAsync(user.Email);
        userManager.Setup(x => x.GetUserNameAsync(user)).ReturnsAsync(user.UserName);

        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(MockBehavior.Strict);
        claimsFactory
            .Setup(x => x.CreateAsync(user))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity(storedClaims, AuthenticationType)));

        return new AvatarProfileService(userManager.Object, claimsFactory.Object, avatarService);
    }
}
