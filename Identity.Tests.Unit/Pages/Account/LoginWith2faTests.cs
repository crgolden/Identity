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
    public static TheoryData<bool, string?> ValidUserCases() => new()
    {
        { false, null },
        { true, Generated.NewLocalPath() },
        { false, string.Empty },
        { true, Generated.NewWhitespaceValue() },
        { false, Generated.NewOverlongValue() },
        { true, Generated.NewControlAndSymbolValue() },
    };

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

    [Theory]
    [MemberData(nameof(ValidUserCases))]
    public async Task OnGetAsync_ValidUser_SetsPropertiesAndReturnsPageResult(bool rememberMe, string? returnUrl)
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId(), UserName = Generated.NewUserName() };
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(user);

        var model = new LoginWith2fa(signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(rememberMe, returnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.Equal(rememberMe, model.RememberMe);
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
}
