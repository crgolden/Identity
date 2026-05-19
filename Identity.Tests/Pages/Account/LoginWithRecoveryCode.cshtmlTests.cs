#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;
using Infrastructure;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
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
        null,
        string.Empty,
        " ",
        "/account/manage?return=true",
        "/path/with/special?param=\ufffd\ufffd\ufffd\ufffdd\ufffd&x=1",

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

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            new Mock<UserManager<IdentityUser<Guid>>>(Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null).Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(twoFactorUser);

        var userManager = new Mock<UserManager<IdentityUser<Guid>>>(Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null).Object;
        var logger = Mock.Of<ILogger<LoginWithRecoveryCodeModel>>();

        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManager, logger);

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
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();

        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);

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
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);

        var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();

        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "code" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync(null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);

        // Ensure TwoFactorRecoveryCodeSignInAsync was not called
        signInManagerMock.Verify(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoginWithRecoveryCodeModel_Constructor_AllNulls_DoesNotThrowAndDefaults()
    {
        // Arrange
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        UserManager<IdentityUser<Guid>>? userManager = null;
        ILogger<LoginWithRecoveryCodeModel>? logger = null;

        // Act
        var exception = Record.Exception(() => new LoginWithRecoveryCodeModel(signInManager, userManager, logger));
        var model = new LoginWithRecoveryCodeModel(signInManager, userManager, logger);

        // Assert
        Assert.Null(exception);
        Assert.NotNull(model);
        Assert.NotNull(model.Input);
        Assert.Null(model.ReturnUrl);
    }

    [Fact]
    public void LoginWithRecoveryCodeModel_Constructor_WithLoggerMock_DoesNotThrowAndDefaults()
    {
        // Arrange
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        UserManager<IdentityUser<Guid>>? userManager = null;
        var loggerMock = new Mock<ILogger<LoginWithRecoveryCodeModel>>();
        var logger = loggerMock.Object;

        // Act
        var exception = Record.Exception(() => new LoginWithRecoveryCodeModel(signInManager, userManager, logger));
        var model = new LoginWithRecoveryCodeModel(signInManager, userManager, logger);

        // Assert
        Assert.Null(exception);
        Assert.NotNull(model);
        Assert.NotNull(model.Input);
        Assert.Null(model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_Succeeded_RedirectsToRoot()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync("user-id");
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("ABCD1234")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>());
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "ABCD 1234" };
        var mockUrl = new Mock<IUrlHelper>();
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
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync("user-id");
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("code")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>());
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
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync("user-id");
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signInManagerMock.Setup(s => s.TwoFactorRecoveryCodeSignInAsync("badcode")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var model = new LoginWithRecoveryCodeModel(signInManagerMock.Object, userManagerMock.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>());
        model.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "badcode" };

        // Act
        var result = await model.OnPostAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }
}