namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Identity.Pages.Account.Manage;
using Infrastructure;
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

        // Act
        var model = new EnableAuthenticatorModel(userManager, urlEncoderMock.Object);

        // Assert
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
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains(expectedId, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var urlEncoder = UrlEncoder.Default;

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("ABCDEFG");
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(TestValues.NewEmailAddress());
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, urlEncoder);
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
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("ABCDEFG");
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(TestValues.NewEmailAddress());
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        var rawCode = "12 34-56";
        var stripped = rawCode
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

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
        Assert.Equal("Verification code is invalid.", error.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_ValidTokenAndNoRecoveryCodesLeft_RedirectsToShowRecoveryCodes()
    {
        // Arrange
        var freshlyGeneratedCodes = new[]
        {
            TestValues.NewRecoveryCode(),
            TestValues.NewRecoveryCode(),
            TestValues.NewRecoveryCode()
        };
        var userManagerMock = MockUserManagerForVerifiedAuthenticator(
            existingRecoveryCount: 0,
            freshlyGeneratedCodes);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, UrlEncoder.Default)
        {
            Input = new EnableAuthenticatorModel.InputModel { Code = BuildVerificationCode() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ShowRecoveryCodes", redirect.PageName);
        Assert.Equal("Your authenticator app has been verified.", model.StatusMessage);
        Assert.Equal(freshlyGeneratedCodes, model.RecoveryCodes);
        userManagerMock.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), true), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ValidTokenAndRecoveryCodesRemaining_RedirectsToTwoFactorAuthentication()
    {
        // Arrange
        var remainingRecoveryCount = Random.Shared.Next(1, 11);
        var userManagerMock = MockUserManagerForVerifiedAuthenticator(
            remainingRecoveryCount,
            [TestValues.NewRecoveryCode()]);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, UrlEncoder.Default)
        {
            Input = new EnableAuthenticatorModel.InputModel { Code = BuildVerificationCode() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./TwoFactorAuthentication", redirect.PageName);
        Assert.Equal("Your authenticator app has been verified.", model.StatusMessage);
        Assert.Empty(model.RecoveryCodes);
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
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedId)], "TestAuth"));
        pageModel.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFound.Value);
    }

    private static string BuildVerificationCode() => TestValues.NewVerificationCode();

    private static Mock<UserManager<IdentityUser<Guid>>> MockUserManagerForVerifiedAuthenticator(
        int existingRecoveryCount,
        string[] freshlyGeneratedCodes)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var enablingUser = new IdentityUser<Guid>
        {
            Id = TestValues.NewUserId(),
            UserName = TestValues.NewUserName(),
            Email = TestValues.NewEmailAddress()
        };

        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(enablingUser);
        userManagerMock.Setup(um => um.VerifyTwoFactorTokenAsync(
                It.IsAny<IdentityUser<Guid>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), true))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(enablingUser.Id.ToString());
        userManagerMock.Setup(um => um.CountRecoveryCodesAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(existingRecoveryCount);
        userManagerMock.Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(It.IsAny<IdentityUser<Guid>>(), 10))
            .ReturnsAsync(freshlyGeneratedCodes);
        userManagerMock.Setup(um => um.GetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(TestValues.NewAuthenticatorKey());
        userManagerMock.Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(enablingUser.Email);
        userManagerMock.Setup(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);

        return userManagerMock;
    }
}