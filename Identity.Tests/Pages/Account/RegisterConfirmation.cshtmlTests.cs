#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;
using Infrastructure;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RegisterConfirmationModelTests
{
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
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

    [Fact]
    public void Constructor_MissingUserManager_DescribeExpectedBehavior()
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);

        // Act
        var model = new RegisterConfirmationModel(mockUserManager.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnGetAsync_EmailIsNull_RedirectsToIndex()
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        var model = new RegisterConfirmationModel(mockUserManager.Object);

        // Act
        var result = await model.OnGetAsync(email: null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Theory]
    [InlineData("nonexistent@example.com")]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult_ForVariousEmails(string email)
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockUserStore, null, null, null, null, null, null, null, null);
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser<Guid>?)null);
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