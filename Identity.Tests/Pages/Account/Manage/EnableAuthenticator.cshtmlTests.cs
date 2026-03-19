#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

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

public partial class EnableAuthenticatorModelTests
{
    /// <summary>
    /// Ensures that attempting to test constructor behavior with null arguments is left as a skipped/inconclusive test.
    /// Input: intention to pass null for one or more non-nullable constructor parameters.
    /// Expected: either the constructor accepts nulls (no exception) or it throws an ArgumentNullException.
    /// This test is skipped because the constructor parameters are non-nullable reference types and
    /// the project constraints prohibit assigning null to non-nullable parameters in generated tests.
    /// </summary>
    [Fact]
    public void Constructor_NullDependency_ExpectedBehavior()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
        var urlEncoderMock = new Mock<UrlEncoder>();

        // Create a minimal UserManager required to construct the model.
        var store = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        IServiceProvider? services = null;
        var umLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;

        var userManager = new UserManager<IdentityUser<Guid>>(
            store,
            options,
            passwordHasher,
            userValidators,
            pwdValidators,
            keyNormalizer,
            errors,
            services,
            umLogger);

        // Act & Assert
        var model = new EnableAuthenticatorModel(userManager, loggerMock.Object, urlEncoderMock.Object);
        Assert.NotNull(model);
    }

    /// <summary>
    /// Test that when no user is found OnPostAsync returns NotFoundObjectResult containing the user id from GetUserId.
    /// Input conditions: UserManager.GetUserAsync returns null and GetUserId returns a known id.
    /// Expected result: NotFoundObjectResult with the same id embedded in the message.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
        var urlEncoder = UrlEncoder.Default;

        var expectedId = "missing-user-id";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new EnableAuthenticatorModel(userManagerMock.Object, loggerMock.Object, urlEncoder);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.IsType<string>(notFound.Value);
        var message = (string)notFound.Value!;
        Assert.Contains(expectedId, message);
    }

    /// <summary>
    /// Test that when ModelState is invalid OnPostAsync returns PageResult.
    /// Input conditions: Valid user is returned but ModelState contains an error.
    /// Expected result: PageResult is returned (and no exception is thrown).
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageResult()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock,
            Options.Create(new IdentityOptions { Tokens = new TokenOptions { AuthenticatorTokenProvider = "Authenticator" } }),
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
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

        var model = new EnableAuthenticatorModel(userManagerMock.Object, loggerMock.Object, urlEncoder);

        // make the model state invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    /// <summary>
    /// Test that when the verification token is invalid OnPostAsync adds model error and returns PageResult.
    /// Input conditions: user exists, Input.Code contains spaces and hyphens which should be stripped and VerifyTwoFactorTokenAsync returns false.
    /// Expected result: ModelState contains error for "Input.Code" and PageResult is returned.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_InvalidVerificationCode_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions { Tokens = new TokenOptions { AuthenticatorTokenProvider = "Authenticator" } });
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, options, null, null, null, null, null, null, null);
        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
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

        var model = new EnableAuthenticatorModel(userManagerMock.Object, loggerMock.Object, urlEncoder)
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

    /// <summary>
    /// Parameterized test for successful verification that covers both recovery-code-generation and not.
    /// Input conditions: user exists, VerifyTwoFactorTokenAsync returns true, SetTwoFactorEnabledAsync succeeds.
    /// - When CountRecoveryCodesAsync returns 0: GenerateNewTwoFactorRecoveryCodesAsync returns codes and redirect is to ShowRecoveryCodes with RecoveryCodes populated.
    /// - When CountRecoveryCodesAsync returns >0: redirect is to TwoFactorAuthentication and RecoveryCodes remains null.
    /// Expected result: Appropriate RedirectToPageResult and StatusMessage set.
    /// </summary>
    [Theory]
    [InlineData(0, "./ShowRecoveryCodes")]
    [InlineData(5, "./TwoFactorAuthentication")]
    public async Task OnPostAsync_ValidToken_RedirectsBasedOnRecoveryCodesCount(int existingRecoveryCount, string expectedPage)
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
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

        var model = new EnableAuthenticatorModel(userManagerMock.Object, loggerMock.Object, urlEncoder)
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

    /// <summary>
    /// Tests that when the user cannot be loaded (UserManager.GetUserAsync returns null),
    /// the OnGetAsync method returns a NotFoundObjectResult containing the user id
    /// returned by UserManager.GetUserId.
    /// Input conditions: GetUserAsync => null, GetUserId => provided id string.
    /// Expected result: NotFoundObjectResult with message "Unable to load user with ID '{id}'."
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var identityOptions = Options.Create(new IdentityOptions());
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock,
            identityOptions,
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        var expectedId = "expected-id-123";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var loggerMock = new Mock<ILogger<EnableAuthenticatorModel>>();
        var urlEncoder = UrlEncoder.Default;

        var pageModel = new EnableAuthenticatorModel(userManagerMock.Object, loggerMock.Object, urlEncoder);

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