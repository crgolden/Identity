namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
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
public class TwoFactorAuthenticationModelTests
{
    public static TheoryData<string?, bool, bool, int> GetOnGetAsyncCases() => new()
    {
        // Cover typical, boundary and unusual numeric values as RecoveryCodesLeft, and both null/non-null authenticator key.
        { null, false, false, 0 },
        { "auth-key-abc", true, true, 5 },
        { "k", false, true, int.MaxValue },
        { null, true, false, int.MinValue },
    };

    [Fact]
    public void Properties_SetAfterConstruction_ReflectAssignedValues()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);

        var model = new TwoFactorAuthenticationModel(userManager.Object, signInManager.Object);

        // Act
        model.HasAuthenticator = true;
        model.RecoveryCodesLeft = 5;
        model.Is2faEnabled = true;
        model.IsMachineRemembered = true;
        model.StatusMessage = "status";

        // Assert
        Assert.True(model.HasAuthenticator);
        Assert.Equal(5, model.RecoveryCodesLeft);
        Assert.True(model.Is2faEnabled);
        Assert.True(model.IsMachineRemembered);
        Assert.Equal("status", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new TwoFactorAuthenticationModel(userManagerMock.Object, signInManagerMock.Object);

        // Prepare ClaimsPrincipal for the PageContext (User)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        const string expectedUserId = "missing-user-id";
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
        Assert.Equal($"Unable to load user with ID '{expectedUserId}'.", notFound.Value);
        signInManagerMock.Verify(s => s.ForgetTwoFactorClientAsync(), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UserFound_ForgetsClientAndRedirectsAndSetsStatusMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new TwoFactorAuthenticationModel(userManagerMock.Object, signInManagerMock.Object);

        // Prepare ClaimsPrincipal for the PageContext (User)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
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
        Assert.Equal("The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.", model.StatusMessage);
        signInManagerMock.Verify(sm => sm.ForgetTwoFactorClientAsync(), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        // Setup: GetUserAsync returns null and GetUserId returns a specific id string
        const string expectedId = "expected-id-123";
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var pageModel = new TwoFactorAuthenticationModel(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFoundResult.Value);
    }

    [Theory]
    [MemberData(nameof(GetOnGetAsyncCases))]
    public async Task OnGetAsync_UserFound_SetsPropertiesAndReturnsPageResult(string? authenticatorKey, bool is2faEnabled, bool isMachineRemembered, int recoveryCodes)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var user = new IdentityUser<Guid>();

        // Setup manager behaviors according to parameters
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

        var pageModel = new TwoFactorAuthenticationModel(mockUserManager.Object, mockSignInManager.Object);

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