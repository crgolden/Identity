namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ChangePasswordTests
{
    private static readonly string CurrentPassword = Generated.NewPassword();

    private static readonly string ReplacementPassword = Generated.NewPassword();

    private static readonly string MissingUserId = Generated.NewUserId().ToString();

    private static readonly string PasswordRejectionReason = Generated.NewFailureReason();

    public static IEnumerable<object?[]> GetUserIdValues()
    {
        yield return
        [
            Generated.NewUserId().ToString()
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
        var model = new ChangePassword(userManagerMock.Object, signInManagerMock.Object);

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ChangePassword(userManagerMock.Object, signInManagerMock.Object);

        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var expectedId = Generated.NewUserId().ToString();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedId);
        model.Input = new ChangePassword.InputModel { OldPassword = CurrentPassword, NewPassword = ReplacementPassword };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(expectedId), notFound.Value);
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
        var userManagerLogger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, identityOptions, passwordHasher, userValidators, passwordValidators, lookupNormalizer, errorDescriber, serviceProvider, userManagerLogger);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict).Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInManagerLogger = NullLogger<SignInManager<IdentityUser<Guid>>>.Instance;
        var schemes = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, httpContextAccessor, claimsFactory, identityOptions, signInManagerLogger, schemes, confirmation);

        // Act
        var model = new ChangePassword(userManagerMock.Object, signInManagerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        var model = Create(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains(MissingUserId, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_UserHasNoPassword_RedirectsToSetPassword()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(false);

        var model = Create(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ChangePassword.SetPasswordPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserHasPassword_ReturnsPage()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(true);

        var model = Create(userManagerMock.Object, signInManagerMock.Object);

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
        var model = Create(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePassword.InputModel { OldPassword = null, NewPassword = ReplacementPassword };

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
        var failedResult = IdentityResult.Failed(new IdentityError { Description = PasswordRejectionReason });
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, CurrentPassword, ReplacementPassword))
            .ReturnsAsync(failedResult);

        var model = Create(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePassword.InputModel { OldPassword = CurrentPassword, NewPassword = ReplacementPassword };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState.Values.SelectMany(v => v.Errors), e => string.Equals(e.ErrorMessage, PasswordRejectionReason, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnPostAsync_ChangePasswordSucceeds_SetsStatusMessageAndRedirects()
    {
        // Arrange
        var (userManagerMock, signInManagerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, CurrentPassword, ReplacementPassword))
            .ReturnsAsync(IdentityResult.Success);
        signInManagerMock
            .Setup(sm => sm.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var model = Create(userManagerMock.Object, signInManagerMock.Object);
        model.Input = new ChangePassword.InputModel { OldPassword = CurrentPassword, NewPassword = ReplacementPassword };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        signInManagerMock.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
        Assert.Equal(ChangePassword.PasswordChangedMessage, model.StatusMessage);
        Assert.IsType<RedirectToPageResult>(result);
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> UserManager,
                    Mock<SignInManager<IdentityUser<Guid>>> SignInManager) CreateMocks()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        return (userManagerMock, signInManagerMock);
    }

    private static ChangePassword Create(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        var model = new ChangePassword(userManager, signInManager);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() },
        };
        return model;
    }
}
