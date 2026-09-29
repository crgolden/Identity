namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RegisterConfirmationTests
{
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var userStore = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>(MockBehavior.Strict).Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>(MockBehavior.Loose).Object;
        var logger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;
        var userManager = new UserManager<IdentityUser<Guid>>(userStore, options, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger);

        // Act
        var model = new RegisterConfirmation(userManager);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Constructor_MissingUserManager_DescribeExpectedBehavior()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        // Act
        var model = new RegisterConfirmation(mockUserManager.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnGetAsync_EmailIsNull_RedirectsToIndex()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();
        var model = new RegisterConfirmation(mockUserManager.Object);

        // Act
        var result = await model.OnGetAsync(email: null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.Home, redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var email = Generated.NewEmailAddress();
        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser<Guid>?)null);
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        var model = new RegisterConfirmation(mockUserManager.Object);
        model.Url = mockUrl.Object;

        // Act
        var result = await model.OnGetAsync(email);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUserByEmail(email), notFound.Value);
        Assert.Null(model.Email);
    }

    [Fact]
    public async Task OnGetAsync_UserFoundWithNoReturnUrl_SetsPropertiesAndDoesNotGenerateConfirmationUrl()
    {
        // Arrange
        var testEmail = Generated.NewEmailAddress();
        var (model, mockUrl) = BuildModelForFoundUser(testEmail);

        // Act
        var result = await model.OnGetAsync(testEmail);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(testEmail, model.Email);
        mockUrl.Verify(u => u.Content(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_UserFoundWithLocalReturnUrl_SetsPropertiesAndDoesNotGenerateConfirmationUrl()
    {
        // Arrange
        var testEmail = Generated.NewEmailAddress();
        var localReturnUrl = Generated.NewLocalPath();
        var (model, mockUrl) = BuildModelForFoundUser(testEmail);

        // Act
        var result = await model.OnGetAsync(testEmail, localReturnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(testEmail, model.Email);
        mockUrl.Verify(u => u.Content(It.IsAny<string>()), Times.Never);
    }

    private static (RegisterConfirmation Model, Mock<IUrlHelper> MockUrl) BuildModelForFoundUser(string email)
    {
        var user = new IdentityUser<Guid>();
        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(It.Is<string>(s => s == email))).ReturnsAsync(user);
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.Content(It.IsAny<string>())).Throws(new Exception("Url.Content should not be called"));

        var model = new RegisterConfirmation(mockUserManager.Object)
        {
            Url = mockUrl.Object
        };
        return (model, mockUrl);
    }
}
