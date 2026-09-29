namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginWith2faTests
{
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var model = new LoginWith2fa(signInManagerMock.Object)
        {
            Input = new LoginWith2fa.InputModel
            {
                TwoFactorCode = Generated.NewVerificationCode()
            }
        };

        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync(true, Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.GetTwoFactorAuthenticationUserAsync(), Times.Never);
        signInManagerMock.Verify(s => s.TwoFactorAuthenticatorSignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginWith2fa(signInManagerMock.Object)
        {
            Input = new LoginWith2fa.InputModel
            {
                TwoFactorCode = Generated.NewVerificationCode()
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        model.Url = urlHelperMock.Object;

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync(false));

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(UserMessages.UnableToLoadTwoFactorUser, ex.Message);
    }

    [Fact]
    public async Task OnGetAsync_UserIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new LoginWith2fa(signInManagerMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(false));

        // Assert
        Assert.Equal(UserMessages.UnableToLoadTwoFactorUser, ex.Message);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserNotRememberedWithNullReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(false, null);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, null);
        Assert.False(model.RememberMe);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserNotRememberedWithBlankReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var blankReturnUrl = Generated.NewBlank();
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(false, blankReturnUrl);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, blankReturnUrl);
        Assert.False(model.RememberMe);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserRememberedWithLocalReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var localReturnUrl = Generated.NewLocalPath();
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(true, localReturnUrl);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, localReturnUrl);
        Assert.True(model.RememberMe);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserRememberedWithWhitespaceReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var whitespaceReturnUrl = Generated.NewWhitespaceValue();
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(true, whitespaceReturnUrl);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, whitespaceReturnUrl);
        Assert.True(model.RememberMe);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserNotRememberedWithOverlongReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var overlongReturnUrl = Generated.NewOverlongValue();
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(false, overlongReturnUrl);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, overlongReturnUrl);
        Assert.False(model.RememberMe);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserRememberedWithControlAndSymbolReturnUrl_SetsPropertiesAndReturnsPageResult()
    {
        // Arrange
        var controlAndSymbolReturnUrl = Generated.NewControlAndSymbolValue();
        var model = BuildModelForTwoFactorUser();

        // Act
        var result = await model.OnGetAsync(true, controlAndSymbolReturnUrl);

        // Assert
        AssertReturnsPageWithReturnUrl(result, model, controlAndSymbolReturnUrl);
        Assert.True(model.RememberMe);
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrowAndInitializesDefaults()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();

        // Act
        var model = new LoginWith2fa(signInManagerMock.Object);

        // Assert
        Assert.NotNull(model);
        Assert.NotNull(model.Input);
        Assert.False(model.RememberMe);
        Assert.Null(model.ReturnUrl);
    }

    [Fact]
    public void Constructor_TwoInstances_InitializesDefaultsIndependently()
    {
        // Arrange
        var firstSignInManagerMock = CreateSignInManagerMock();
        var secondSignInManagerMock = CreateSignInManagerMock();

        // Act
        var firstModel = new LoginWith2fa(firstSignInManagerMock.Object);
        var secondModel = new LoginWith2fa(secondSignInManagerMock.Object);

        // Assert
        Assert.NotSame(firstModel.Input, secondModel.Input);
        Assert.False(firstModel.RememberMe);
        Assert.False(secondModel.RememberMe);
        Assert.Null(firstModel.ReturnUrl);
        Assert.Null(secondModel.ReturnUrl);
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var user = new IdentityUser<Guid>();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock
            .Setup(s => s.TwoFactorAuthenticatorSignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var model = new LoginWith2fa(signInManagerMock.Object)
        {
            Input = new LoginWith2fa.InputModel
            {
                TwoFactorCode = Generated.NewVerificationCode(), RememberMachine = false
            }
        };
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        model.Url = urlHelperMock.Object;

        // Act
        var result = await model.OnPostAsync(false);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingLockout, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var user = new IdentityUser<Guid>();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock
            .Setup(s => s.TwoFactorAuthenticatorSignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var model = new LoginWith2fa(signInManagerMock.Object)
        {
            Input = new LoginWith2fa.InputModel
            {
                TwoFactorCode = Generated.NewVerificationCode(), RememberMachine = false
            }
        };
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        model.Url = urlHelperMock.Object;

        // Act
        var result = await model.OnPostAsync(false);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    private static Mock<SignInManager<IdentityUser<Guid>>> CreateSignInManagerMock()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        return MockHelpers.MockSignInManager(userManagerMock.Object);
    }

    private static LoginWith2fa BuildModelForTwoFactorUser()
    {
        var signInManagerMock = CreateSignInManagerMock();
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId(), UserName = Generated.NewUserName() };
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(user);
        return new LoginWith2fa(signInManagerMock.Object);
    }

    private static void AssertReturnsPageWithReturnUrl(IActionResult result, LoginWith2fa model, string? returnUrl)
    {
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnUrl, model.ReturnUrl);
    }
}
