namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginWithRecoveryCodeTests
{
    private const char QueryStringStart = '?';
    private const char QueryStringAssignment = '=';

    private static readonly string KnownRecoveryCode = Generated.NewRecoveryCode();

    private static readonly string UnknownRecoveryCode = Generated.NewRecoveryCode();

    [Fact]
    public async Task OnGetAsync_TwoFactorUserExistsWithNoReturnUrl_LeavesReturnUrlNullAndReturnsPage()
    {
        // Arrange
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync();

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, null);
    }

    [Fact]
    public async Task OnGetAsync_TwoFactorUserExistsWithLocalReturnUrlAndQuery_SetsReturnUrlAndReturnsPage()
    {
        // Arrange
        var localPath = Generated.NewLocalPath();
        var queryName = Generated.NewPathSegment();
        var queryValue = Generated.NewPathSegment();
        var returnUrlWithQuery = WithQuery(localPath, queryName, queryValue);
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(returnUrlWithQuery);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, returnUrlWithQuery);
    }

    [Fact]
    public async Task OnGetAsync_TwoFactorUserExistsWithNestedReturnUrlAndControlAndSymbolQueryValue_SetsReturnUrlAndReturnsPage()
    {
        // Arrange
        var parentPath = Generated.NewLocalPath();
        var nestedPath = Generated.NewLocalPath();
        var queryName = Generated.NewPathSegment();
        var controlAndSymbolQueryValue = Generated.NewControlAndSymbolValue();
        var nestedReturnUrlWithQuery = WithQuery(parentPath + nestedPath, queryName, controlAndSymbolQueryValue);
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(nestedReturnUrlWithQuery);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, nestedReturnUrlWithQuery);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var model = new LoginWithRecoveryCode(signInManagerMock.Object);
        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());
        model.Input = new LoginWithRecoveryCode.InputModel { RecoveryCode = Generated.NewRecoveryCode() };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.GetTwoFactorAuthenticationUserAsync(), Times.Never);
        signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new LoginWithRecoveryCode(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCode.InputModel { RecoveryCode = KnownRecoveryCode };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(UserMessages.UnableToLoadTwoFactorUser, ex.Message);
        signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginWithRecoveryCode(signInManagerMock.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGetAsync());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(UserMessages.UnableToLoadTwoFactorUser, ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_Succeeded_RedirectsToRoot()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync(KnownRecoveryCode)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var model = new LoginWithRecoveryCode(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCode.InputModel
        {
            RecoveryCode = Generated.WithEmbeddedWhitespace(KnownRecoveryCode)
        };
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns(false);
        model.Url = mockUrl.Object;

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(PageRoutes.ContentRoot, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_Succeeded_RedirectsToTheRequestedLocalReturnUrl()
    {
        // Arrange
        var returnUrl = new Uri(Generated.NewLocalPath(), UriKind.Relative);
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(new IdentityUser<Guid>());
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync(KnownRecoveryCode)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.IsLocalUrl(returnUrl.OriginalString)).Returns(true);
        var model = new LoginWithRecoveryCode(signInManagerMock.Object)
        {
            Input = new LoginWithRecoveryCode.InputModel { RecoveryCode = KnownRecoveryCode },
            Url = mockUrl.Object,
        };

        // Act
        var result = await model.OnPostAsync(returnUrl);

        // Assert
        Assert.Equal(returnUrl.OriginalString, Assert.IsType<LocalRedirectResult>(result).Url);
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync(KnownRecoveryCode)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = new LoginWithRecoveryCode(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCode.InputModel { RecoveryCode = KnownRecoveryCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingLockout, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync(UnknownRecoveryCode)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var model = new LoginWithRecoveryCode(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCode.InputModel { RecoveryCode = UnknownRecoveryCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    private static Mock<SignInManager<IdentityUser<Guid>>> CreateSignInManagerMock()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        return MockHelpers.MockSignInManager(userManagerMock.Object);
    }

    private static LoginWithRecoveryCode BuildModelForTwoFactorUser()
    {
        var twoFactorUser = new IdentityUser<Guid> { Id = Generated.NewUserId(), UserName = Generated.NewUserName() };
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(twoFactorUser);
        return new LoginWithRecoveryCode(signInManagerMock.Object);
    }

    private static Uri WithQuery(string path, string queryName, string queryValue) =>
        new(path + QueryStringStart + queryName + QueryStringAssignment + queryValue, UriKind.Relative);

    private static void AssertReturnsPageWithReturnUrl(IActionResult result, LoginWithRecoveryCode model, Uri? returnUrl)
    {
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnUrl?.OriginalString, model.ReturnUrl?.OriginalString);
    }
}
