namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
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
public class ChangePasswordModelTests
{
    public static IEnumerable<object?[]> GetUserIdValues()
    {
        // Provide a concrete string and a null value to exercise both message forms
        yield return
        [
            "test-user-123"
        ];
        yield return
        [
            null
        ];
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Put a dummy principal on the PageContext (not strictly used because we expect early exit)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        // Make model invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure GetUserAsync was never called due to early return
        userManagerMock.Verify(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Prepare a principal and page context
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Setup UserManager to return null user and a known id
        const string expectedId = "expected-user-id";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedId);

        // Set Input so the null/whitespace guard is bypassed and we reach the user lookup
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFound.Value);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Loose).Object;
        var userManagerLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, identityOptions, passwordHasher, userValidators, passwordValidators, lookupNormalizer, errorDescriber, serviceProvider, userManagerLogger);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict).Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInManagerLogger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemes = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, httpContextAccessor, claimsFactory, identityOptions, signInManagerLogger, schemes, confirmation);

        // Act
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("some-user-id");

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("some-user-id", notFound.Value?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_UserHasNoPassword_RedirectsToSetPassword()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(false);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./SetPassword", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserHasPassword_ReturnsPage()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(true);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_NullOldPassword_ReturnsPageWithoutCallingGetUser()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = null, NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ChangePasswordFails_ReturnsPageWithModelErrors()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        var failedResult = IdentityResult.Failed(new IdentityError { Description = "Password too weak." });
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, "OldP@ss1!", "NewP@ss1!"))
            .ReturnsAsync(failedResult);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState.Values.SelectMany(v => v.Errors), e => e.ErrorMessage == "Password too weak.");
    }

    [Fact]
    public async Task OnPostAsync_ChangePasswordSucceeds_SetsStatusMessageAndRedirects()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, "OldP@ss1!", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Success);
        signInManagerMock
            .Setup(sm => sm.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        signInManagerMock.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
        Assert.Equal("Your password has been changed.", model.StatusMessage);
        Assert.IsType<RedirectToPageResult>(result);
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> userManager,
                    Mock<SignInManager<IdentityUser<Guid>>> signInManager) CreateMocks()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        return (userManagerMock, signInManagerMock);
    }

    private static ChangePasswordModel CreateModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        var model = new ChangePasswordModel(userManager, signInManager);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() },
        };
        return model;
    }
}
