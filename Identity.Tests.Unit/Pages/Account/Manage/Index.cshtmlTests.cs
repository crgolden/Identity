namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ManageIndexModelTests
{
    public static TheoryData<string?, string?> ValidUserData() => new()
    {
        { "normalUser", "+1234567890" },
        { string.Empty, string.Empty },
        { "   ", "??-?est" },
        { new string('a', 500), null },
    };

    public static TheoryData<string?, string?> PhoneNumbersThatNeedNoUpdate()
    {
        var storedPhoneNumber = BuildPhoneNumber();
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

        const string expectedId = "expected-id-123";
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedId);
        var model = new IndexModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedId}'.";
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
            Id = Guid.NewGuid()
        };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync(returnedUserName);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(returnedPhoneNumber);
        var model = new IndexModel(userManagerMock.Object, signInManagerMock.Object);

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
        var expectedUserId = "expected-user-id";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedUserId);
        var page = new IndexModel(userManagerMock.Object, signInManagerMock.Object);

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
            Id = Guid.NewGuid()
        };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var page = new IndexModel(userManagerMock.Object, signInManagerMock.Object);
        page.ModelState.AddModelError("Input.PhoneNumber", "Invalid phone");
        page.Input = new IndexModel.InputModel
        {
            PhoneNumber = "000"
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
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhone);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, inputPhone);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Your profile has been updated", page.StatusMessage);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PhoneNumberChangedAndSetSucceeds_SetsPhoneNumberAndRefreshesSignIn()
    {
        // Arrange
        var existingPhoneNumber = BuildPhoneNumber();
        var replacementPhoneNumber = BuildPhoneNumber();
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhoneNumber);
        userManagerMock.Setup(u => u.SetPhoneNumberAsync(user, replacementPhoneNumber)).ReturnsAsync(IdentityResult.Success);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, replacementPhoneNumber);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Your profile has been updated", page.StatusMessage);
        userManagerMock.Verify(u => u.SetPhoneNumberAsync(user, replacementPhoneNumber), Times.Once);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PhoneNumberChangedAndSetFails_ReportsErrorAndDoesNotRefreshSignIn()
    {
        // Arrange
        var existingPhoneNumber = BuildPhoneNumber();
        var rejectedPhoneNumber = BuildPhoneNumber();
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetPhoneNumberAsync(user)).ReturnsAsync(existingPhoneNumber);
        userManagerMock
            .Setup(u => u.SetPhoneNumberAsync(user, rejectedPhoneNumber))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = $"rejected-{Guid.NewGuid():N}" }));
        signInManagerMock.Setup(s => s.RefreshSignInAsync(user)).Returns(Task.CompletedTask);

        var page = BuildPostPage(userManagerMock, signInManagerMock, rejectedPhoneNumber);

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Unexpected error when trying to set phone number.", page.StatusMessage);
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
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
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
        var model = new IndexModel(userManager, signInManager);

        // Assert
        Assert.NotNull(model);
    }

    private static string BuildPhoneNumber() =>
        $"+1{Random.Shared.Next(2000000000, int.MaxValue)}";

    private static IndexModel BuildPostPage(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock,
        string? inputPhone) =>
        new(userManagerMock.Object, signInManagerMock.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Input = new IndexModel.InputModel { PhoneNumber = inputPhone }
        };
}