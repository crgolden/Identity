namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResetAuthenticatorModelTests
{
    [Fact]
    public async Task OnGet_UserExists_ReturnsPage()
    {
        // Arrange
        var model = BuildOnGetModel(signedInUser: new IdentityUser<Guid>(), resolvedUserId: null);

        // Act
        var result = await model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGet_UserMissing_ReturnsNotFoundNamingTheUserId()
    {
        // Arrange
        var missingUserId = Guid.NewGuid().ToString();
        var model = BuildOnGetModel(signedInUser: null, resolvedUserId: missingUserId);

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{missingUserId}'.", notFound.Value as string);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage()
    {
        // Arrange
        var userIdString = "missing-user-id";
        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userIdString);

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userIdString}'.", notFound.Value);
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>()), Times.Never);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ResetSucceeds_ResetsKeyAndRedirectsToEnableAuthenticator()
    {
        // Arrange
        var (model, mockUserManager, mockSignInManager, resettingUser) =
            BuildOnPostFixture(IdentityResult.Success);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        AssertResetAndRedirected(result, model, mockUserManager, mockSignInManager, resettingUser);
    }

    [Fact]
    public async Task OnPostAsync_ResetFails_StillResetsKeyAndRedirectsToEnableAuthenticator()
    {
        // Arrange
        var failedReset = IdentityResult.Failed(new IdentityError { Description = $"reset-failed-{Guid.NewGuid():N}" });
        var (model, mockUserManager, mockSignInManager, resettingUser) = BuildOnPostFixture(failedReset);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        AssertResetAndRedirected(result, model, mockUserManager, mockSignInManager, resettingUser);
    }

    private static void AssertResetAndRedirected(
        IActionResult result,
        ResetAuthenticatorModel model,
        Mock<UserManager<IdentityUser<Guid>>> mockUserManager,
        Mock<SignInManager<IdentityUser<Guid>>> mockSignInManager,
        IdentityUser<Guid> resettingUser)
    {
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./EnableAuthenticator", redirect.PageName);
        Assert.Equal(
            "Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.",
            model.StatusMessage);
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(resettingUser, false), Times.Once);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(resettingUser), Times.Once);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(resettingUser), Times.Once);
    }

    private static ResetAuthenticatorModel BuildOnGetModel(IdentityUser<Guid>? signedInUser, string? resolvedUserId)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(signedInUser);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(resolvedUserId);

        var signInManager = MockHelpers.MockSignInManager(userManagerMock.Object);

        return new ResetAuthenticatorModel(userManagerMock.Object, signInManager.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private static (
        ResetAuthenticatorModel Model,
        Mock<UserManager<IdentityUser<Guid>>> UserManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> SignInManagerMock,
        IdentityUser<Guid> ResettingUser) BuildOnPostFixture(IdentityResult resetOutcome)
    {
        var resettingUser = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@example.com"
        };

        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(resettingUser);
        mockUserManager
            .Setup(um => um.SetTwoFactorEnabledAsync(resettingUser, false))
            .ReturnsAsync(resetOutcome);
        mockUserManager
            .Setup(um => um.ResetAuthenticatorKeyAsync(resettingUser))
            .ReturnsAsync(resetOutcome);

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);
        mockSignInManager
            .Setup(sm => sm.RefreshSignInAsync(resettingUser))
            .Returns(Task.CompletedTask);

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        return (model, mockUserManager, mockSignInManager, resettingUser);
    }
}