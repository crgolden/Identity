namespace Identity.Tests.Unit.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConfirmEmailChangeTests
{
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>(MockBehavior.Strict).Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>(MockBehavior.Loose).Object;
        var loggerUserManager = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;
        var userManager = new UserManager<IdentityUser<Guid>>(storeMock.Object, options, passwordHasher, userValidators, pwdValidators, keyNormalizer, errors, services, loggerUserManager);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignIn = NullLogger<SignInManager<IdentityUser<Guid>>>.Instance;
        var schemes = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManager = new SignInManager<IdentityUser<Guid>>(userManager, httpContextAccessor.Object, claimsFactory.Object, options, loggerSignIn, schemes, confirmation);

        // Act
        var model = new ConfirmEmailChange(userManager, signInManager);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnGetAsync_NullUserId_RedirectsToIndex()
    {
        // Arrange
        var email = Generated.NewEmailAddress();
        var code = Generated.NewEmailConfirmationToken();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(null, email, code);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_NullEmail_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var code = Generated.NewEmailConfirmationToken();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, null, code);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_NullCode_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewEmailAddress();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, email, null);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewEmailAddress();
        var token = Generated.NewProviderKey();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync((IdentityUser<Guid>?)null);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(userId), notFound.Value);
    }

    [Fact]
    public async Task OnGetAsync_ChangeEmailFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewEmailAddress();
        var token = Generated.NewProviderKey();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() }));
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmailChange.EmailChangeFailedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_SetUserNameFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewEmailAddress();
        var token = Generated.NewProviderKey();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() }));
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmailChange.UserNameChangeFailedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_AllOperationsSucceed_RefreshesSignInAndSetsSuccessMessage()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewEmailAddress();
        var token = Generated.NewProviderKey();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask).Verifiable();
        var model = new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmailChange.EmailChangeConfirmedMessage, model.StatusMessage);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(u => u == user)), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_EmptyEmail_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var token = Generated.NewEmailConfirmationToken();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, Generated.NewBlank(), encoded);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceEmail_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var whitespaceEmail = Generated.NewWhitespaceValue();
        var token = Generated.NewEmailConfirmationToken();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, whitespaceEmail, encoded);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_SpecialCharacterEmail_ProceedsAndReturnSuccess()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var email = Generated.NewNonAsciiEmailAddress();
        var token = Generated.NewEmailConfirmationToken();
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask);
        var model = new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmailChange.EmailChangeConfirmedMessage, model.StatusMessage);
    }

    private static ConfirmEmailChange BuildModel()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        return new ConfirmEmailChange(userManagerMock.Object, signInManagerMock.Object);
    }

    private static void AssertRedirectsToIndex(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.Home, redirect.PageName);
    }
}
