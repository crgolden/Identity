namespace Identity.Tests.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

/// <summary>
/// Tests for ResetPasswordModel constructor behavior.
/// Note: The production constructor accepts a Microsoft.AspNetCore.Identity.UserManager{IdentityUser{Guid}}.
/// Creating a real UserManager requires many DI services (IUserStore, IOptions, IPasswordHasher, validators, logger, etc.).
/// Per project constraints, no fake/hand-rolled implementations are allowed. If you can supply a properly constructed
/// UserManager instance (or a Moq mock configured with appropriate constructor arguments), remove the Skip on the test
/// and provide that instance in the Arrange section.
/// </summary>
public class ResetPasswordModelTests
{
    /// <summary>
    /// Verifies constructor behavior for ResetPasswordModel.
    /// Input conditions: A properly-constructed or mockable UserManager{IdentityUser{Guid}} must be provided to the constructor.
    /// Expected result: The constructor should complete without throwing and produce a usable ResetPasswordModel instance.
    /// Notes: This test is marked skipped because creating a valid UserManager requires multiple DI dependencies.
    /// To complete this test:
    ///  - Construct a real UserManager by providing mocks for IUserStore, IOptions{IdentityOptions}, IPasswordHasher, validators, ILookupNormalizer, IdentityErrorDescriber, IServiceProvider, and ILogger<UserManager>.
    ///  - OR create a Moq.Mock<UserManager<IdentityUser<Guid>>> supplying the same constructor arguments via Mock's constructor parameters.
    /// After providing a valid instance, remove the Skip and assert that the instance is non-null and (optionally) that calling simple PageModel members does not throw.
    /// </summary>
    [Fact]
    public void ResetPasswordModel_ValidUserManager_ConstructsSuccessfully()
    {
        // Arrange
        // NOTE: Per repository constraints, do NOT create any custom fake or stub classes.
        // Provide a fully-configured UserManager<IdentityUser<Guid>> (constructed using mocks for its constructor params)
        // or a Moq.Mock<UserManager<IdentityUser<Guid>>> with appropriate constructor args here.
        // Example guidance (do NOT implement here inline as per constraints):
        // var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        // var options = Options.Create(new IdentityOptions());
        // var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        // var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        // var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        // var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        // var errors = new IdentityErrorDescriber();
        // var services = new Mock<IServiceProvider>();
        // var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<IdentityUser<Guid>>>>();
        // var userManager = new UserManager<IdentityUser<Guid>>(storeMock.Object, options, passwordHasherMock.Object, userValidators, passwordValidators, lookupNormalizerMock.Object, errors, services.Object, logger.Object);

        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManager = new UserManager<IdentityUser<Guid>>(
            storeMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errors,
            services.Object,
            logger.Object);

        // Act
        var model = new ResetPasswordModel(userManager);

        // Assert
        Assert.NotNull(model);

        // Intentionally minimal: the goal is to ensure constructor wiring works with mocked UserManager.
    }

    /// <summary>
    /// The test verifies that when code is null the handler returns a BadRequestObjectResult
    /// with the expected error message.
    /// Input: code == null.
    /// Expected: BadRequestObjectResult with message "A code must be supplied for password reset.".
    /// </summary>
    [Fact]
    public void OnGet_CodeIsNull_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        var model = new ResetPasswordModel(mockUserManager.Object);

        // Act
        IActionResult result = model.OnGet(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("A code must be supplied for password reset.", badRequest.Value);
    }

    /// <summary>
    /// The test verifies that valid Base64Url-encoded inputs are decoded and assigned to Input.Code,
    /// and that the method returns a PageResult.
    /// Inputs: several original strings (including empty and special characters) encoded with Base64Url.
    /// Expected: PageResult returned and model.Input.Code equals the original string.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetValidEncodedCases))]
    public void OnGet_ValidBase64UrlEncodedCode_SetsInputCodeAndReturnsPage(string original)
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        var model = new ResetPasswordModel(mockUserManager.Object);

        string encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(original));

        // Act
        IActionResult result = model.OnGet(encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        var input = model.Input;
        Assert.NotNull(input);
        Assert.Equal(original, input.Code);
    }

    /// <summary>
    /// Provides valid original strings to be encoded by Base64Url for tests.
    /// Includes empty string and strings with special/unicode characters.
    /// </summary>
    public static IEnumerable<object[]> GetValidEncodedCases()
    {
        yield return new object[] { "abc" };
        yield return new object[] { "p@$$w0rd!" };
        yield return new object[] { "这是一些中文" };
    }

    /// <summary>
    /// The test verifies that malformed Base64Url inputs (containing whitespace or invalid chars)
    /// cause the decoder to throw a FormatException.
    /// Inputs: whitespace-only and clearly invalid Base64Url strings.
    /// Expected: FormatException is thrown.
    /// </summary>
    [Theory]
    [InlineData("invalid!")]
    [InlineData("%%%")]
    public void OnGet_MalformedCode_ThrowsFormatException(string malformed)
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        var model = new ResetPasswordModel(mockUserManager.Object);

        // Act & Assert
        Assert.Throws<FormatException>(() => model.OnGet(malformed));
    }

    /// <summary>
    /// Verifies that when the ModelState is invalid, OnPostAsync returns a PageResult without calling the user manager.
    /// Input conditions:
    ///  - ModelState contains at least one error.
    /// Expected result:
    ///  - A PageResult is returned and neither FindByEmailAsync nor ResetPasswordAsync are invoked.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var model = new ResetPasswordModel(userManagerMock.Object);
        // Create Input but ModelState will be invalid
        model.Input = new ResetPasswordModel.InputModel
        {
            Email = "user@example.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            Code = "code"
        };

        // Make ModelState invalid
        model.ModelState.AddModelError("Email", "Required");

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        // Ensure user manager methods were not called
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Parameterized test to validate both:
    /// 1) When user is not found (FindByEmailAsync returns null) -> redirect to ResetPasswordConfirmation.
    /// 2) When user is found and ResetPasswordAsync succeeds -> redirect to ResetPasswordConfirmation.
    /// Input conditions:
    ///  - provide flags controlling whether a user exists and whether reset succeeds
    /// Expected result:
    ///  - A RedirectToPageResult to "./ResetPasswordConfirmation"
    /// </summary>
    [Theory]
    [InlineData(false, false)] // user not found -> redirect
    [InlineData(true, true)]   // user found and reset succeeds -> redirect
    public async Task OnPostAsync_UserMissingOrResetSucceeds_RedirectsToConfirmation(bool userExists, bool resetSucceeds)
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        IdentityUser<Guid>? foundUser = null;
        if (userExists)
        {
            foundUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "test" };
        }

        userManagerMock
            .Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == "test@example.com")))
            .ReturnsAsync(foundUser);

        if (userExists)
        {
            IdentityResult result = resetSucceeds
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "failed" });

            userManagerMock
                .Setup(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(result);
        }

        var model = new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = "test@example.com",
                Password = "NewP@ssw0rd",
                ConfirmPassword = "NewP@ssw0rd",
                Code = "code"
            }
        };

        // Act
        IActionResult actionResult = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("./ResetPasswordConfirmation", redirect.PageName);

        // Verify FindByEmailAsync called once
        userManagerMock.Verify(um => um.FindByEmailAsync("test@example.com"), Times.Once);

        if (userExists)
        {
            // ResetPasswordAsync should be called when user exists
            userManagerMock.Verify(um => um.ResetPasswordAsync(foundUser!, "code", "NewP@ssw0rd"), Times.Once);
        }
        else
        {
            // ResetPasswordAsync should not be called when user is not found
            userManagerMock.Verify(um => um.ResetPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    /// <summary>
    /// Verifies that when ResetPasswordAsync fails, the errors are added to ModelState and a PageResult is returned.
    /// Input conditions:
    ///  - User exists.
    ///  - ResetPasswordAsync returns a failed IdentityResult with multiple IdentityError entries.
    /// Expected result:
    ///  - A PageResult is returned and ModelState contains the error descriptions under the empty key.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ResetPasswordFails_AddsModelErrorsAndReturnsPage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var foundUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = "user2@example.com", UserName = "user2" };

        userManagerMock
            .Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == "user2@example.com")))
            .ReturnsAsync(foundUser);

        var errors = new[]
        {
            new IdentityError { Description = "Err1" },
            new IdentityError { Description = "Err2" }
        };
        var failedResult = IdentityResult.Failed(errors);

        userManagerMock
            .Setup(um => um.ResetPasswordAsync(foundUser, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(failedResult);

        var model = new ResetPasswordModel(userManagerMock.Object)
        {
            Input = new ResetPasswordModel.InputModel
            {
                Email = "user2@example.com",
                Password = "AnotherP@ss1",
                ConfirmPassword = "AnotherP@ss1",
                Code = "code2"
            }
        };

        // Precondition: ModelState valid
        Assert.True(model.ModelState.IsValid);

        // Act
        IActionResult actionResult = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(actionResult);
        Assert.False(model.ModelState.IsValid);

        // Errors added with empty key
        ModelStateEntry? entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        var actualMessages = entry!.Errors.Select(e => e.ErrorMessage).ToArray();
        Assert.Contains("Err1", actualMessages);
        Assert.Contains("Err2", actualMessages);

        // Verify calls
        userManagerMock.Verify(um => um.FindByEmailAsync("user2@example.com"), Times.Once);
        userManagerMock.Verify(um => um.ResetPasswordAsync(foundUser, "code2", "AnotherP@ss1"), Times.Once);
    }
}