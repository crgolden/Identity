namespace Identity.Tests.Pages.Account;
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
public class LoginWith2faModelTests
{
    public static TheoryData<bool, string?> ValidUserCases() => new()
    {
        { false, null },
        { true, "/" },
        { false, string.Empty },
        { true, "   " },
        { false, new string('a', 1024) }, // long string
        { true, "special-chars-!@#$%^&*()\t\n" },
    };

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var model = new LoginWith2faModel(signInManagerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel
            {
                TwoFactorCode = "000000",
                RememberMachine = false
            }
        };

        // Make model state invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync(true, "/irrelevant");

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no calls to authentication flows when model invalid
        signInManagerMock.Verify(s => s.GetTwoFactorAuthenticationUserAsync(), Times.Never);
        signInManagerMock.Verify(s => s.TwoFactorAuthenticatorSignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginWith2faModel(signInManagerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel
            {
                TwoFactorCode = "123456",
                RememberMachine = false
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await model.OnPostAsync(false, null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    [Fact]
    public async Task OnGetAsync_UserIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new LoginWith2faModel(signInManagerMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(false, null));

        // Assert
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(ValidUserCases))]
    public async Task OnGetAsync_ValidUser_SetsPropertiesAndReturnsPageResult(bool rememberMe, string? returnUrl)
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser" };
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(user);

        var model = new LoginWith2faModel(signInManagerMock.Object);

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
        var model = new LoginWith2faModel(signInManagerMock.Object);

        // Assert
        Assert.NotNull(model);

        // Default expectations
        Assert.NotNull(model.Input);
        Assert.False(model.RememberMe);
        Assert.Null(model.ReturnUrl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Constructor_MultipleValidDependencies_NoExceptionAndIndependentDefaults(int instances)
    {
        // Arrange & Act
        var models = new List<LoginWith2faModel>();
        for (var i = 0; i < instances; i++)
        {
            var signInManagerMock = CreateSignInManagerMock();
            var model = new LoginWith2faModel(signInManagerMock.Object);
            models.Add(model);
        }

        // Assert
        Assert.Equal(instances, models.Count);
        foreach (var model in models)
        {
            Assert.NotNull(model);
            Assert.NotNull(model.Input);
            Assert.False(model.RememberMe);
            Assert.Null(model.ReturnUrl);
        }
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

        var model = new LoginWith2faModel(signInManagerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel { TwoFactorCode = "123456", RememberMachine = false }
        };
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Act
        var result = await model.OnPostAsync(false, null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirect.PageName);
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

        var model = new LoginWith2faModel(signInManagerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel { TwoFactorCode = "000000", RememberMachine = false }
        };
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Act
        var result = await model.OnPostAsync(false, null);

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
