namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class SetPasswordModelTests
{
    [Fact]
    public void Constructor_WithNonNullDependencies_NotImplemented()
    {
        // Arrange
        // Create the minimal dependencies required to construct concrete UserManager and SignInManager instances.
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var identityErrorDescriber = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>(MockBehavior.Loose).Object;
        var userManagerLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;

        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            identityErrorDescriber,
            services,
            userManagerLogger);

        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict).Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInLogger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object;
        var userConfirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManager,
            httpContextAccessor,
            claimsFactory,
            options,
            signInLogger,
            schemeProvider,
            userConfirmation);

        // Act
        var exception = Record.Exception(() => new SetPasswordModel(userManager, signInManager));

        // Assert
        Assert.Null(exception);
        var model = new SetPasswordModel(userManager, signInManager);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var pageModel = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Make model state invalid
        pageModel.ModelState.AddModelError("Test", "Invalid");

        // Act
        var result = await pageModel.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no user manager/signin calls happened
        userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        userManagerMock.Verify(u => u.AddPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var expectedUserId = "user-123";
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var pageModel = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Set Input so the null/whitespace guard is bypassed and we reach the user lookup
        pageModel.Input = new SetPasswordModel.InputModel { NewPassword = "NewP@ss1!" };

        // Act
        var result = await pageModel.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal($"Unable to load user with ID '{expectedUserId}'.", message);
        userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdInMessage()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        const string expectedId = "expected-user-id";
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new SetPasswordModel(mockUserManager.Object, mockSignInManager.Object);

        // Provide a PageContext with a principal (content of principal is irrelevant due to setups)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = Assert.IsType<string>(notFoundResult.Value);
        Assert.Contains(expectedId, value);
        Assert.Contains("Unable to load user with ID", value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetAsync_ExistingUser_BehavesBasedOnHasPassword(bool hasPassword)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var user = new IdentityUser<Guid>();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(um => um.HasPasswordAsync(user))
            .ReturnsAsync(hasPassword);

        var model = new SetPasswordModel(mockUserManager.Object, mockSignInManager.Object);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        if (hasPassword)
        {
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./ChangePassword", redirect.PageName);
        }
        else
        {
            Assert.IsType<PageResult>(result);
        }
    }

    [Fact]
    public async Task OnPostAsync_AddPasswordFails_AddsModelErrorsAndReturnsPage()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(u => u.AddPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new SetPasswordModel.InputModel { NewPassword = "weak" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Password too weak."));
    }

    [Fact]
    public async Task OnPostAsync_AddPasswordSucceeds_RefreshesSignInAndRedirects()
    {
        // Arrange
        var user = new IdentityUser<Guid>();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.AddPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var model = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new SetPasswordModel.InputModel { NewPassword = "ValidP@ss1!" };
        model.TempData = new Mock<ITempDataDictionary>(MockBehavior.Strict).Object;

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }
}