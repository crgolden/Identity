namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class TwoFactorAuthenticationTests
{
    private static readonly string AuthenticatorKey = Generated.NewAuthenticatorKey();

    private static readonly string StatusText = Generated.NewValidationMessage();

    public static TheoryData<string?, bool, bool, int> GetOnGetAsyncCases() => new()
    {
        { null, false, false, 0 },
        { AuthenticatorKey, true, true, Generated.NewRecoveryCodeCount() },
        { Generated.NewAuthenticatorKey(), false, true, int.MaxValue },
        { null, true, false, int.MinValue },
    };

    [Fact]
    public void Properties_SetAfterConstruction_ReflectAssignedValues()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);

        var model = new TwoFactorAuthentication(userManager.Object, signInManager.Object);

        var recoveryCodesLeft = Generated.NewRecoveryCodeCount();

        // Act
        model.HasAuthenticator = true;
        model.RecoveryCodesLeft = recoveryCodesLeft;
        model.Is2faEnabled = true;
        model.IsMachineRemembered = true;
        model.StatusMessage = StatusText;

        // Assert
        Assert.True(model.HasAuthenticator);
        Assert.Equal(recoveryCodesLeft, model.RecoveryCodesLeft);
        Assert.True(model.Is2faEnabled);
        Assert.True(model.IsMachineRemembered);
        Assert.Equal(StatusText, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new TwoFactorAuthentication(userManagerMock.Object, signInManagerMock.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        var expectedUserId = Generated.NewUserId().ToString();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(expectedUserId), notFound.Value);
        signInManagerMock.Verify(s => s.ForgetTwoFactorClientAsync(), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UserFound_ForgetsClientAndRedirectsAndSetsStatusMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new TwoFactorAuthentication(userManagerMock.Object, signInManagerMock.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        signInManagerMock
            .Setup(sm => sm.ForgetTwoFactorClientAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(TwoFactorAuthentication.BrowserForgottenMessage, model.StatusMessage);
        signInManagerMock.Verify(sm => sm.ForgetTwoFactorClientAsync(), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var expectedId = Generated.NewUserId().ToString();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var pageModel = new TwoFactorAuthentication(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(expectedId), notFoundResult.Value);
    }

    [Theory]
    [MemberData(nameof(GetOnGetAsyncCases))]
    public async Task OnGetAsync_UserFound_SetsPropertiesAndReturnsPageResult(string? authenticatorKey, bool is2faEnabled, bool isMachineRemembered, int recoveryCodes)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var user = new IdentityUser<Guid>();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(um => um.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync(authenticatorKey);
        mockUserManager
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(is2faEnabled);
        mockUserManager
            .Setup(um => um.CountRecoveryCodesAsync(user))
            .ReturnsAsync(recoveryCodes);

        mockSignInManager
            .Setup(sm => sm.IsTwoFactorClientRememberedAsync(user))
            .ReturnsAsync(isMachineRemembered);

        var pageModel = new TwoFactorAuthentication(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(authenticatorKey != null, pageModel.HasAuthenticator);
        Assert.Equal(is2faEnabled, pageModel.Is2faEnabled);
        Assert.Equal(isMachineRemembered, pageModel.IsMachineRemembered);
        Assert.Equal(recoveryCodes, pageModel.RecoveryCodesLeft);
    }
}
