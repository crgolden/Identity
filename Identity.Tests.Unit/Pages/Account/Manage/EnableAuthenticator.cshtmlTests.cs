namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public partial class EnableAuthenticatorModelTests
{
    [Fact]
    public void Constructor_NullDependency_ExpectedBehavior()
    {
        // Arrange
        var urlEncoderMock = new Mock<UrlEncoder>(MockBehavior.Strict);

        var userManager = MockHelpers.MockUserManager().Object;

        // Act & Assert
        var model = new EnableAuthenticatorModel(userManager, urlEncoderMock.Object);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var urlEncoder = UrlEncoder.Default;

        var expectedId = "missing-user-id";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.IsType<string>(notFound.Value);
        var message = (string)notFound.Value!;
        Assert.Contains(expectedId, message);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var urlEncoder = UrlEncoder.Default;

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        // Ensure LoadSharedKeyAndQrCodeUriAsync internal calls succeed:
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("ABCDEFG");
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("email@example.com");
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder);

        // make the model state invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var urlEncoder = UrlEncoder.Default;

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };

        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        // Ensure LoadSharedKeyAndQrCodeUriAsync internal calls succeed:
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("ABCDEFG");
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("email@example.com");
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        // Expect that VerifyTwoFactorTokenAsync receives the stripped token "123456"
        var rawCode = "12 34-56";
        var stripped = rawCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(
                It.IsAny<IdentityUser<Guid>>(),
                It.IsAny<string>(),
                It.Is<string>(s => s == stripped)))
            .ReturnsAsync(false);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder)
        {
            Input = new EnableAuthenticatorModel.InputModel { Code = rawCode }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.ContainsKey("Input.Code"));
        var error = model.ModelState["Input.Code"]?.Errors.FirstOrDefault();
        Assert.NotNull(error);
        Assert.Equal("Verification code is invalid.", error!.ErrorMessage);
    }

    [Theory]
    [InlineData(0, "./ShowRecoveryCodes")]
    [InlineData(5, "./TwoFactorAuthentication")]
    public async Task OnPostAsync_ValidToken_RedirectsBasedOnRecoveryCodesCount(int existingRecoveryCount, string expectedPage)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var urlEncoder = UrlEncoder.Default;

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser", Email = "user@test.com" };

        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(
                It.IsAny<IdentityUser<Guid>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), true))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("the-user-id");
        userManagerMock.Setup(um => um.CountRecoveryCodesAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(existingRecoveryCount);

        var generatedCodes = new[] { "code1", "code2", "code3" };
        userManagerMock.Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(It.IsAny<IdentityUser<Guid>>(), 10))
            .ReturnsAsync(generatedCodes);

        // Ensure LoadSharedKeyAndQrCodeUriAsync internal calls succeed if invoked later (defensive)
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("ABCDEFG");
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("email@example.com");
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder)
        {
            Input = new EnableAuthenticatorModel.InputModel { Code = "123456" }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(expectedPage, redirect.PageName);
        Assert.Equal("Your authenticator app has been verified.", model.StatusMessage);

        if (existingRecoveryCount == 0)
        {
            Assert.NotNull(model.RecoveryCodes);
            Assert.Equal(generatedCodes.Length, model.RecoveryCodes.Length);
            Assert.Equal(generatedCodes, model.RecoveryCodes);
        }
        else
        {
            Assert.Empty(model.RecoveryCodes);
        }

        // Verify that enabling 2FA was attempted
        userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), true), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var expectedId = "expected-id-123";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var urlEncoder = UrlEncoder.Default;

        var pageModel = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder);

        // Provide a principal so PageModel.User is non-null (the mocks use It.IsAny but set up for realism)
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedId)], "TestAuth"));
        pageModel.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFound.Value);
    }
}
