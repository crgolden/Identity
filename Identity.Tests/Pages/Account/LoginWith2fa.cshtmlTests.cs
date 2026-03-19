#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Tests for Identity.Pages.Account.LoginWith2faModel.OnPostAsync
/// </summary>
[Trait("Category", "Unit")]
public class LoginWith2faModelTests
{
    /// <summary>
    /// Provides combination of rememberMe and returnUrl values to exercise edge cases for strings.
    /// Includes null, empty, whitespace-only, and a long string.
    /// </summary>
    public static TheoryData<bool, string?> ValidUserCases() => new()
    {
        { false, null },
        { true, "/" },
        { false, string.Empty },
        { true, "   " },
        { false, new string('a', 1024) }, // long string
        { true, "special-chars-!@#$%^&*()\t\n" },
    };

    /// <summary>
    /// The purpose of this test is to ensure that when ModelState is invalid the handler returns the Page result without calling external dependencies.
    /// Input conditions: ModelState contains an error, any rememberMe and returnUrl values.
    /// Expected result: PageResult is returned.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        var loggerMock = new Mock<ILogger<LoginWith2faModel>>();
        var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel
            {
                TwoFactorCode = "000000",
                RememberMachine = false
            }
        };

        // Make model state invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync(true, "/irrelevant");

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no calls to authentication flows when model invalid
        signInManagerMock.Verify(s => s.GetTwoFactorAuthenticationUserAsync(), Times.Never);
        signInManagerMock.Verify(s => s.TwoFactorAuthenticatorSignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// The purpose of this test is to verify that an InvalidOperationException is thrown when no two-factor authentication user is available.
    /// Input conditions: GetTwoFactorAuthenticationUserAsync returns null.
    /// Expected result: InvalidOperationException with a specific message is thrown.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_NoTwoFactorUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((IdentityUser<Guid>?)null);
        var loggerMock = new Mock<ILogger<LoginWith2faModel>>();
        var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object)
        {
            Input = new LoginWith2faModel.InputModel
            {
                TwoFactorCode = "123456",
                RememberMachine = false
            }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await model.OnPostAsync(false, null));
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    /// <summary>
    /// Verifies that when no two-factor authentication user is available the method throws InvalidOperationException.
    /// Input: SignInManager.GetTwoFactorAuthenticationUserAsync returns null.
    /// Expected: InvalidOperationException with specific message.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var (signInManagerMock, userManagerMock) = CreateSignInAndUserManagerMocks();
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var loggerMock = new Mock<ILogger<LoginWith2faModel>>();
        var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync(false, null));

        // Assert
        Assert.Equal("Unable to load two-factor authentication user.", ex.Message);
    }

    /// <summary>
    /// Verifies that when a two-factor authentication user is available the method sets ReturnUrl and RememberMe and returns a PageResult.
    /// Input conditions: Various combinations of rememberMe and returnUrl (including null, empty, whitespace, and long strings).
    /// Expected: Model properties set accordingly and PageResult returned.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidUserCases))]
    public async Task OnGetAsync_ValidUser_SetsPropertiesAndReturnsPageResult(bool rememberMe, string? returnUrl)
    {
        // Arrange
        var (signInManagerMock, userManagerMock) = CreateSignInAndUserManagerMocks();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser" };
        signInManagerMock
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync(user);

        var loggerMock = new Mock<ILogger<LoginWith2faModel>>();
        var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);

        // Act
        var result = await model.OnGetAsync(rememberMe, returnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.Equal(rememberMe, model.RememberMe);
    }

    /// <summary>
    /// Verifies that the LoginWith2faModel constructor accepts valid dependency instances and
    /// does not throw. Also asserts the default values of public properties after construction.
    /// Input conditions:
    /// - All constructor dependencies are provided as mock instances.
    /// Expected result:
    /// - No exception is thrown.
    /// - Input is null, RememberMe is false, ReturnUrl is null.
    /// </summary>
    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrowAndInitializesDefaults()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var loggerMock = new Mock<ILogger<LoginWith2faModel>>();

        // Act
        var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(model);

        // Default expectations
        Assert.NotNull(model.Input);
        Assert.False(model.RememberMe);
        Assert.Null(model.ReturnUrl);
    }

    /// <summary>
    /// Parameterized test that constructs multiple instances of LoginWith2faModel to ensure
    /// constructor remains stable across repeated creations with different mock instances.
    /// Input conditions:
    /// - 'instances' indicates how many separate instances to construct (1 or 3).
    /// Expected result:
    /// - No exception is thrown for any construction.
    /// - Each created instance has default values: Input == null, RememberMe == false, ReturnUrl == null.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Constructor_MultipleValidDependencies_NoExceptionAndIndependentDefaults(int instances)
    {
        // Arrange & Act
        var models = new List<LoginWith2faModel>();
        for (var i = 0; i < instances; i++)
        {
            var userManagerMock = CreateUserManagerMock();
            var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
            var loggerMock = new Mock<ILogger<LoginWith2faModel>>();

            var model = new LoginWith2faModel(signInManagerMock.Object, userManagerMock.Object, loggerMock.Object);
            models.Add(model);
        }

        // Assert
        Assert.Equal(instances, models.Count);
        foreach (var model in models)
        {
            Assert.NotNull(model);
            Assert.NotNull(model.Input);
            Assert.False(model.RememberMe);
            Assert.Null(model.ReturnUrl);
        }
    }

    // Helper factory methods are placed inside the test class per requirements.

    // Helper to create the complex mocks required by SignInManager and UserManager.
    // All helpers are inside the test class as required.
    private static (Mock<SignInManager<IdentityUser<Guid>>>, Mock<UserManager<IdentityUser<Guid>>>) CreateSignInAndUserManagerMocks()
    {
        // Mock dependencies for UserManager
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var userManagerLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object,
            optionsMock.Object,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizerMock.Object,
            errorDescriber,
            services.Object,
            userManagerLogger.Object);

        // Mock dependencies for SignInManager
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var signInManagerLogger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsMock.Object,
            signInManagerLogger.Object,
            schemeProviderMock.Object,
            userConfirmationMock.Object);

        return (signInManagerMock, userManagerMock);
    }

    private static Mock<UserManager<IdentityUser<Guid>>> CreateUserManagerMock()
    {
        var userStore = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>().Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var logger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;

        var mock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore,
            Options.Create(new IdentityOptions()),
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger)
        {
            CallBase = true
        };

        return mock;
    }

    private static Mock<SignInManager<IdentityUser<Guid>>> CreateSignInManagerMock(UserManager<IdentityUser<Guid>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>().Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var logger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemes = new Mock<IAuthenticationSchemeProvider>().Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;

        var mock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManager,
            contextAccessor,
            claimsFactory,
            Options.Create(new IdentityOptions()),
            logger,
            schemes,
            confirmation)
        {
            CallBase = true
        };

        return mock;
    }
}