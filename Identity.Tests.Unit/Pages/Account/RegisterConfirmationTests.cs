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
    public static TheoryData<string> UnknownEmailAddresses() => new()
    {
        Generated.NewEmailAddress(),
    };

    public static TheoryData<string?> ReturnUrlValues() => new()
    {
        (string?)null,
        Generated.NewLocalPath(),
    };

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

    [Theory]
    [MemberData(nameof(UnknownEmailAddresses))]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult_ForVariousEmails(string email)
    {
        // Arrange
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

    [Theory]
    [MemberData(nameof(ReturnUrlValues))]
    public async Task OnGetAsync_UserFound_SetsPropertiesAndDoesNotGenerateConfirmationUrl_UrlContentBehavior(string? returnUrl)
    {
        // Arrange
        var testEmail = Generated.NewEmailAddress();
        var user = new IdentityUser<Guid>();
        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(It.Is<string>(s => s == testEmail))).ReturnsAsync(user);
        var mockUrl = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrl.Setup(u => u.Content(It.IsAny<string>())).Throws(new Exception("Url.Content should not be called"));

        var model = new RegisterConfirmation(mockUserManager.Object)
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
