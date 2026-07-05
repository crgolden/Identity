namespace Identity.Tests.Unit.Pages.Account;
using Infrastructure;

using Identity.Pages.Account;
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
public class LoginWithRecoveryCodeModelTests
{
    // MemberData providing a variety of string edge cases including null.
    public static TheoryData<string?> ReturnUrlValues() => new()
    {
        (string?)null,
        string.Empty,
        " ",
        "/account/manage?return=true",
        "/path/with/special?param=����d�&x=1",

        // long string (~2048 chars) to test boundary for very long URLs
        new string('a', 2048),
    };

    public static TheoryData<string?, string> GetReturnUrlCases() => new()
    {
        // Case: null returnUrl should redirect to Url.Content("~/") which we mock to "/"
        { null, "/" },

        // Case: provided returnUrl should be used as-is
        { "/some/local/path", "/some/local/path" },
    };

    [Theory]
    [MemberData(nameof(ReturnUrlValues))]
    public async Task OnGetAsync_TwoFactorUserExists_SetsReturnUrlAndReturnsPage(string? returnUrl)
    {
        // Arrange
        var twoFactorUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser" };
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

        // Make ModelState invalid
        model.ModelState.AddModelError("key", "error");
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "irrelevant" };

        // Act
        var result = await model.OnPostAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure sign-in manager methods were not invoked
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
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "code" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync(null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);

        // Ensure TwoFactorRecoveryCodeSignInAsync was not called
        signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_Succeeded_RedirectsToRoot()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("ABCD1234")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "ABCD 1234" };
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns(false);
        model.Url = mockUrl.Object;

        // Act
        var result = await model.OnPostAsync(null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockoutPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("code")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "code" };

        // Act
        var result = await model.OnPostAsync(null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("badcode")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "badcode" };

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
