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
    private static readonly string StatusText = Generated.NewValidationMessage();

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

    [Fact]
    public async Task OnGetAsync_UserWithNothingConfigured_ReportsNoAuthenticatorNo2faAndNoRecoveryCodes()
    {
        // Arrange
        const int noRecoveryCodes = 0;
        var pageModel = BuildModelForFoundUser(authenticatorKey: null, is2faEnabled: false, isMachineRemembered: false, noRecoveryCodes);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(pageModel.HasAuthenticator);
        Assert.False(pageModel.Is2faEnabled);
        Assert.False(pageModel.IsMachineRemembered);
        Assert.Equal(noRecoveryCodes, pageModel.RecoveryCodesLeft);
    }

    [Fact]
    public async Task OnGetAsync_UserWithAuthenticator2faAndRememberedMachine_ReportsAllEnabledWithRecoveryCodesLeft()
    {
        // Arrange
        var authenticatorKey = Generated.NewAuthenticatorKey();
        var recoveryCodesLeft = Generated.NewRecoveryCodeCount();
        var pageModel = BuildModelForFoundUser(authenticatorKey, is2faEnabled: true, isMachineRemembered: true, recoveryCodesLeft);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(pageModel.HasAuthenticator);
        Assert.True(pageModel.Is2faEnabled);
        Assert.True(pageModel.IsMachineRemembered);
        Assert.Equal(recoveryCodesLeft, pageModel.RecoveryCodesLeft);
    }

    [Fact]
    public async Task OnGetAsync_UserWithAuthenticatorAndMaximumRecoveryCodesButNo2fa_ReportsAuthenticatorAndRememberedMachine()
    {
        // Arrange
        var authenticatorKey = Generated.NewAuthenticatorKey();
        var pageModel = BuildModelForFoundUser(authenticatorKey, is2faEnabled: false, isMachineRemembered: true, int.MaxValue);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(pageModel.HasAuthenticator);
        Assert.False(pageModel.Is2faEnabled);
        Assert.True(pageModel.IsMachineRemembered);
        Assert.Equal(int.MaxValue, pageModel.RecoveryCodesLeft);
    }

    [Fact]
    public async Task OnGetAsync_UserWith2faButNoAuthenticatorAndMinimumRecoveryCodes_ReportsNoAuthenticator()
    {
        // Arrange
        var pageModel = BuildModelForFoundUser(authenticatorKey: null, is2faEnabled: true, isMachineRemembered: false, int.MinValue);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(pageModel.HasAuthenticator);
        Assert.True(pageModel.Is2faEnabled);
        Assert.False(pageModel.IsMachineRemembered);
        Assert.Equal(int.MinValue, pageModel.RecoveryCodesLeft);
    }

    private static TwoFactorAuthentication BuildModelForFoundUser(
        string? authenticatorKey,
        bool is2faEnabled,
        bool isMachineRemembered,
        int recoveryCodesLeft)
    {
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
            .ReturnsAsync(recoveryCodesLeft);

        mockSignInManager
            .Setup(sm => sm.IsTwoFactorClientRememberedAsync(user))
            .ReturnsAsync(isMachineRemembered);

        return new TwoFactorAuthentication(mockUserManager.Object, mockSignInManager.Object);
    }
}
