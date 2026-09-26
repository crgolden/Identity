namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    public static TheoryData<string?, string?> ValidUserData() => new()
    {
        { Generated.NewUserName(), Generated.NewPhoneNumber() },
        { string.Empty, string.Empty },
        { Generated.NewWhitespaceValue(), Generated.NewControlAndSymbolValue() },
        { Generated.NewOverlongValue(), null },
    };

    public static TheoryData<string?, string?> PhoneNumbersThatNeedNoUpdate()
    {
        var storedPhoneNumber = Generated.NewPhoneNumber();
        return new TheoryData<string?, string?>
        {
            { null, null },
            { storedPhoneNumber, storedPhoneNumber },
            { storedPhoneNumber, null },
            { null, string.Empty },
        };
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var expectedId = Generated.NewUserId().ToString();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedId);
        var model = new Index(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = UserMessages.UnableToLoadUser(expectedId);
        Assert.Equal(expectedMessage, notFound.Value);
    }

    [Theory]
    [MemberData(nameof(ValidUserData))]
    public async Task OnGetAsync_UserExists_LoadsUsernameAndPhoneAndReturnsPage(string? returnedUserName, string? returnedPhoneNumber)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync(returnedUserName);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(returnedPhoneNumber);
        var model = new Index(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnedUserName, model.Username);
        Assert.NotNull(model.Input);
        Assert.Equal(returnedPhoneNumber, model.Input.PhoneNumber);
        userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        userManagerMock.Verify(u => u.GetUserNameAsync(user), Times.Once);
        userManagerMock.Verify(u => u.GetPhoneNumberAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var expectedUserId = Generated.NewUserId().ToString();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedUserId);
        var page = new Index(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains(expectedUserId, message, StringComparison.Ordinal);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageAndDoesNotChangePhoneOrSignIn()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid>
        {
            Id = Generated.NewUserId()
        };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var page = new Index(userManagerMock.Object, signInManagerMock.Object);
        page.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());
        page.Input = new Index.InputModel
        {
            PhoneNumber = Generated.NewPhoneNumber()
        };

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(u => u.GetPhoneNumberAsync(It.IsAny<IdentityUser<Guid>>()), Times.Once);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(PhoneNumbersThatNeedNoUpdate))]
    public async Task OnPostAsync_PhoneNumberUnchangedOrBlank_RefreshesSignInWithoutSettingPhoneNumber(
        string? existingPhone,
        string? inputPhone)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhone);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, inputPhone);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Index.ProfileUpdatedMessage, page.StatusMessage);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PhoneNumberChangedAndSetSucceeds_SetsPhoneNumberAndRefreshesSignIn()
    {
        // Arrange
        var existingPhoneNumber = Generated.NewPhoneNumber();
        var replacementPhoneNumber = Generated.NewPhoneNumber();
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhoneNumber);
        userManagerMock.Setup(u => u.SetPhoneNumberAsync(user, replacementPhoneNumber)).ReturnsAsync(IdentityResult.Success);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, replacementPhoneNumber);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Index.ProfileUpdatedMessage, page.StatusMessage);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(user, replacementPhoneNumber), Times.Once);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PhoneNumberChangedAndSetFails_ReportsErrorAndDoesNotRefreshSignIn()
    {
        // Arrange
        var existingPhoneNumber = Generated.NewPhoneNumber();
        var rejectedPhoneNumber = Generated.NewPhoneNumber();
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhoneNumber);
        userManagerMock
            .Setup(u => u.SetPhoneNumberAsync(user, rejectedPhoneNumber))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() }));
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, rejectedPhoneNumber);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Index.PhoneNumberUpdateFailedMessage, page.StatusMessage);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(user, rejectedPhoneNumber), Times.Once);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>(MockBehavior.Strict);
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var identityErrorDescriber = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLogger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;
        var userManager = new UserManager<IdentityUser<Guid>>(storeMock.Object, optionsMock.Object, passwordHasherMock.Object, userValidators, pwdValidators, lookupNormalizerMock.Object, identityErrorDescriber, serviceProviderMock.Object, userManagerLogger);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var signInManagerLogger = NullLogger<SignInManager<IdentityUser<Guid>>>.Instance;
        var schemeProviderMock = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict);
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();
        var signInManager = new SignInManager<IdentityUser<Guid>>(userManager, httpContextAccessorMock.Object, claimsFactoryMock.Object, optionsMock.Object, signInManagerLogger, schemeProviderMock.Object, userConfirmationMock.Object);

        // Act
        var model = new Index(userManager, signInManager);

        // Assert
        Assert.NotNull(model);
    }

    private static Index BuildPostPage(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock,
        string? inputPhone) =>
        new(userManagerMock.Object, signInManagerMock.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Input = new Index.InputModel { PhoneNumber = inputPhone }
        };
}
