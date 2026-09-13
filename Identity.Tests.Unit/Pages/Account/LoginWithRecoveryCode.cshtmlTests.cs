namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginWithRecoveryCodeModelTests
{
    private const char QueryStringStart = '?';
    private const char QueryStringAssignment = '=';

    private static readonly string KnownRecoveryCode = TestValues.NewRecoveryCode();

    private static readonly string UnknownRecoveryCode = TestValues.NewRecoveryCode();

    public static TheoryData<string?> ReturnUrlValues() => new()
    {
        (string?)null,
        string.Empty,
        TestValues.NewWhitespaceValue(),
        TestValues.NewLocalPath() + QueryStringStart + TestValues.NewPathSegment() + QueryStringAssignment +
            TestValues.NewPathSegment(),
        TestValues.NewLocalPath() + TestValues.NewLocalPath() + QueryStringStart + TestValues.NewPathSegment() +
            QueryStringAssignment + TestValues.NewControlAndSymbolValue(),
        TestValues.NewOverlongValue(),
    };

    [Theory]
    [MemberData(nameof(ReturnUrlValues))]
    public async Task OnGetAsync_TwoFactorUserExists_SetsReturnUrlAndReturnsPage(string? returnUrl)
    {
        // Arrange
        var twoFactorUser = new IdentityUser<Guid> { Id = TestValues.NewUserId(), UserName = TestValues.NewUserName() };
        var signInManagerMock = CreateSignInManagerMock();

        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(twoFactorUser);

        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(returnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnUrl, model.ReturnUrl);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.ModelState.AddModelError(TestValues.NewModelStateKey(), TestValues.NewValidationMessage());
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = TestValues.NewRecoveryCode() };

        // Act
        var result = await model.OnPostAsync(null);

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

        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = KnownRecoveryCode };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync(null));

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
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGetAsync(null));

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
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel
        {
            RecoveryCode = TestValues.WithEmbeddedWhitespace(KnownRecoveryCode)
        };
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns(false);
        model.Url = mockUrl.Object;

        // Act
        var result = await model.OnPostAsync(null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(PageRoutes.ContentRoot, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync(KnownRecoveryCode)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = KnownRecoveryCode };

        // Act
        var result = await model.OnPostAsync(null);

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
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = UnknownRecoveryCode };

        // Act
        var result = await model.OnPostAsync(null);

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