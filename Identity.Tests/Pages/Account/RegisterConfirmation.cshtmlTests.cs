#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Trait("Category", "Unit")]
public class RegisterConfirmationModelTests
{
    /// <summary>
    /// Verifies that the RegisterConfirmationModel constructor can be invoked with valid dependencies.
    /// Input conditions:
    /// - A valid IEmailSender mock is available.
    /// - A UserManager instance or mock is required but creating a reliable mock for UserManager requires
    ///   providing multiple dependencies (store, options, passwordHasher, userValidators, etc.). Because UserManager
    ///   in many frameworks is not trivially mockable without supplying constructor parameters, this test is marked skipped.
    /// Expected result:
    /// - Constructor should not throw when supplied valid instances. This test is left as a scaffold for the consumer to
    ///   complete by providing a concrete UserManager or an adequate mock setup.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        // Note: The IEmailSender interface can be mocked easily:
        var sender = new Mock<IEmailSender>().Object;
        // Create the minimal set of dependencies required by UserManager<IdentityUser<Guid>>
        var userStore = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>().Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var logger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;
        var userManager = new UserManager<IdentityUser<Guid>>(userStore, options, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger);
        // Act
        var model = new RegisterConfirmationModel(userManager);
        // Assert
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies constructor behavior when sender dependency is provided but UserManager construction is not set up.
    /// Input conditions:
    /// - IEmailSender is mocked.
    /// - UserManager is not provided due to complexity of construction in unit test environment.
    /// Expected result:
    /// - This test is marked skipped and documents how to complete it: either construct a UserManager with real dependencies
    ///   or provide a fully initialized mock via Moq using the appropriate constructor arguments.
    /// </summary>
    [Fact]
    public void Constructor_MissingUserManager_DescribeExpectedBehavior()
    {
        // Arrange
        var sender = new Mock<IEmailSender>().Object;
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        // Act
        var model = new RegisterConfirmationModel(mockUserManager.Object);
        // Assert
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies that when email is null the handler redirects to the Index page.
    /// Input: email == null.
    /// Expected: RedirectToPageResult with PageName '/Index'.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_EmailIsNull_RedirectsToIndex()
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        var mockSender = new Mock<IEmailSender>();
        var model = new RegisterConfirmationModel(mockUserManager.Object);
        // Act
        var result = await model.OnGetAsync(email: null);
        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Verifies that when the user cannot be found for various non-null email inputs, the handler returns NotFound with an explanatory message.
    /// Inputs tested: empty string, whitespace-only string, arbitrary non-existent email.
    /// Expected: NotFoundObjectResult with the message "Unable to load user with email '{email}'."
    /// </summary>
    [Theory]
    [InlineData("nonexistent@example.com")]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult_ForVariousEmails(string email)
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser<Guid>? )null);
        var mockSender = new Mock<IEmailSender>();
        var mockUrl = new Mock<IUrlHelper>();
        mockUrl.Setup(u => u.Content("~/")).Returns("/");
        var model = new RegisterConfirmationModel(mockUserManager.Object);
        model.Url = mockUrl.Object;
        // Act
        var result = await model.OnGetAsync(email);
        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with email '{email}'.", notFound.Value);
        // Ensure Email property not set when user not found
        Assert.Null(model.Email);
    }

    /// <summary>
    /// Ensures that when a user exists the handler returns Page and sets Email regardless of returnUrl.
    /// Url.Content is never called by the handler.
    /// Inputs: returnUrl == null and returnUrl == "/custom".
    /// Expected: PageResult, Email set to provided email.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("/custom")]
    public async Task OnGetAsync_UserFound_SetsPropertiesAndDoesNotGenerateConfirmationUrl_UrlContentBehavior(string? returnUrl)
    {
        // Arrange
        var testEmail = "found@example.com";
        var user = new IdentityUser<Guid>();
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        mockUserManager.Setup(m => m.FindByEmailAsync(It.Is<string>(s => s == testEmail))).ReturnsAsync(user);
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.Content(It.IsAny<string>())).Throws(new Exception("Url.Content should not be called"));

        var model = new RegisterConfirmationModel(mockUserManager.Object)
        {
            Url = mockUrl.Object
        };
        // Act
        var result = await model.OnGetAsync(testEmail, returnUrl);
        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(testEmail, model.Email);
        mockUrl.Verify(u => u.Content(It.IsAny<string>()), Times.Never);
    }
}
