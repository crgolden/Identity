namespace Identity.Tests.Pages.Account;
using Infrastructure;

using System.Text;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConfirmEmailChangeModelTests
{
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        // NOTE: To implement this test, construct or mock UserManager<IdentityUser<Guid>> and
        // SignInManager<IdentityUser<Guid>> with valid constructor parameters (stores, options, etc.).
        // Example approaches:
        // - Use Moq to mock only the virtual members and supply required constructor args to Mock<UserManager<...>>(...) if feasible.
        // - Create a real UserManager with an in-memory IUserStore implementation provided by your test environment.
        //
        // For safety and portability we do not attempt to instantiate these framework classes here.
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>(MockBehavior.Strict).Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>(MockBehavior.Loose).Object;
        var loggerUserManager = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;
        var userManager = new UserManager<IdentityUser<Guid>>(storeMock.Object, options, passwordHasher, userValidators, pwdValidators, keyNormalizer, errors, services, loggerUserManager);
        var httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignIn = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemes = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManager = new SignInManager<IdentityUser<Guid>>(userManager, httpContextAccessor.Object, claimsFactory.Object, options, loggerSignIn, schemes, confirmation);

        // Act
        var model = new ConfirmEmailChangeModel(userManager, signInManager);

        // Assert
        Assert.NotNull(model);

        // Additional behavioral assertions may be added once dependencies can be provided.
    }

    [Theory]
    [InlineData(null, "user@example.com", "code")]
    [InlineData("userId", null, "code")]
    [InlineData("userId", "user@example.com", null)]
    public async Task OnGetAsync_NullParameters_RedirectsToIndex(string? userId, string? email, string? code)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, code);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const string userId = "missing-user";
        const string email = "user@example.com";
        const string token = "tok";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync((IdentityUser<Guid>?)null);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userId}'.", notFound.Value);
    }

    [Fact]
    public async Task OnGetAsync_ChangeEmailFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        const string userId = "user-1";
        const string email = "new@example.com";
        const string token = "change-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Error changing email.", model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_SetUserNameFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        const string userId = "user-2";
        const string email = "newuser@example.com";
        const string token = "token-2";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "set user name failed" }));
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Error changing user name.", model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_AllOperationsSucceed_RefreshesSignInAndSetsSuccessMessage()
    {
        // Arrange
        const string userId = "user-3";
        const string email = "ok@example.com";
        const string token = "ok-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask).Verifiable();
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Thank you for confirming your email change.", model.StatusMessage);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(u => u == user)), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnGetAsync_EmptyOrWhitespaceEmail_RedirectsToIndex(string email)
    {
        // Arrange
        const string userId = "user-4";
        const string token = "var-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_SpecialCharacterEmail_ProceedsAndReturnSuccess()
    {
        // Arrange
        const string userId = "user-4";
        const string email = "user+special@ex\u00E4mple.com";
        const string token = "var-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Thank you for confirming your email change.", model.StatusMessage);
    }
}