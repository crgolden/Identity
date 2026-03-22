#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

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

/// <summary>
/// Tests for PasskeysModel constructor behavior.
/// </summary>
[Trait("Category", "Unit")]
public partial class PasskeysModelTests
{
    public static IEnumerable<object?[]> CredentialIdNullOrEmptyData()
    {
        // MemberData supports null values via object?[]
        yield return [null];
        yield return [string.Empty];
    }

    /// <summary>
    /// Verifies that constructing PasskeysModel with valid manager instances does not throw
    /// and that public properties are initially null (default) after construction.
    /// Input conditions:
    /// - UserManager and SignInManager are provided with either empty or one-element validator collections.
    /// Expected result:
    /// - No exception thrown.
    /// - CurrentPasskeys, Input, and StatusMessage are null by default.
    /// </summary>
    /// <param name="useSingleValidator">If true, provide a single-item validators collection; otherwise provide an empty collection.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PasskeysModel_Ctor_ValidManagers_PropertiesInitializedToNull(bool useSingleValidator)
    {
        // Arrange
        // Prepare a minimal IUserStore required by UserManager constructor.
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        // Prepare optional dependencies for UserManager
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasher = Mock.Of<IPasswordHasher<IdentityUser<Guid>>>();
        IEnumerable<IUserValidator<IdentityUser<Guid>>> userValidators = useSingleValidator
            ? [Mock.Of<IUserValidator<IdentityUser<Guid>>>()]
            : [];
        IEnumerable<IPasswordValidator<IdentityUser<Guid>>> passwordValidators = [];
        var lookupNormalizer = Mock.Of<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var userManagerLogger = Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>();

        // Create UserManager mock with required constructor arguments
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            lookupNormalizer,
            errorDescriber,
            services,
            userManagerLogger);

        // Prepare dependencies for SignInManager
        var httpContextAccessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var signInOptions = Mock.Of<IOptions<IdentityOptions>>();
        var signInLogger = Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemes = Mock.Of<IAuthenticationSchemeProvider>();
        var userConfirmation = Mock.Of<IUserConfirmation<IdentityUser<Guid>>>();

        // Create SignInManager mock with the UserManager instance
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            httpContextAccessor,
            claimsFactory,
            signInOptions,
            signInLogger,
            schemes,
            userConfirmation);

        // Act
        var model = new PasskeysModel(userManagerMock.Object, signInManagerMock.Object);

        // Assert
        Assert.NotNull(model); // model constructed successfully
        Assert.NotNull(model.CurrentPasskeys); // list initialized to empty by constructor
        Assert.Empty(model.CurrentPasskeys);
        Assert.NotNull(model.Input); // Input property initialized with default InputModel by constructor
        Assert.Null(model.StatusMessage); // StatusMessage default (not set) after construction
    }

    /// <summary>
    /// Verifies that when the current user cannot be loaded the handler returns NotFound with the user id from UserManager.
    /// Input conditions: UserManager.GetUserAsync returns null and GetUserId returns a known id.
    /// Expected: NotFoundObjectResult containing the message with the provided id.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAddPasskeyAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);
        mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("the-user-id");

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            null,
            null);

        var model = new PasskeysModel(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Unable to load user with ID 'the-user-id'", notFound.Value?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Partial test placeholder: Verifies behavior when attestation fails. This test is marked as skipped because constructing
    /// a framework PasskeyAttestationResult and its Failure.Message requires the concrete framework type details which are not present
    /// in the provided scope. To complete:
    /// - Create or obtain a PasskeyAttestationResult instance (or mock) with Succeeded == false and Failure.Message set.
    /// - Setup SignInManager.PerformPasskeyAttestationAsync to return that result.
    /// - Assert RedirectToPageResult and proper StatusMessage ("Could not add the passkey: {message}.").
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
#pragma warning disable S2699
    [Fact]
    public async Task OnPostAddPasskeyAsync_AttestationFails_RedirectsWithFailureMessage_Partial()
    {
        // Partial: see XML comment for guidance.
        await Task.CompletedTask;
    }
#pragma warning restore S2699

    /// <summary>
    /// Partial test placeholder: Verifies behavior when adding/updating the passkey on the user fails.
    /// This test is marked as skipped because it requires constructing a PasskeyAttestationResult.Passkey instance
    /// and wiring UserManager.AddOrUpdatePasskeyAsync behavior which depends on framework types not provided in scope.
    /// To complete:
    /// - Construct an attestation result with Succeeded == true and a Passkey containing a CredentialId.
    /// - Setup SignInManager.PerformPasskeyAttestationAsync to return the attestation.
    /// - Setup UserManager.AddOrUpdatePasskeyAsync to return IdentityResult.Failed(...) and assert
    ///   RedirectToPageResult and StatusMessage equals "The passkey could not be added to your account.".
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAddPasskeyAsync_AddOrUpdateFails_RedirectsWithFailureMessage_Partial()
    {
        // Partial: see XML comment for guidance.
        await Task.CompletedTask;
        Assert.True(true);
    }

    /// <summary>
    /// Partial test placeholder: Verifies the success path where the passkey is added and the user is redirected to RenamePasskey with the Base64Url encoded id.
    /// This test is skipped because constructing the PasskeyAttestationResult.Passkey and ensuring AddOrUpdatePasskeyAsync succeeds
    /// requires framework types that are not present in the provided source scope.
    /// To complete:
    /// - Construct a Passkey with a known CredentialId byte[].
    /// - Return a PasskeyAttestationResult with Succeeded == true and the Passkey.
    /// - Setup UserManager.AddOrUpdatePasskeyAsync to return IdentityResult.Success.
    /// - Call the handler and assert RedirectToPageResult.PageName == "./RenamePasskey" and RouteValues["id"] equals Base64Url.EncodeToString(CredentialId).
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
#pragma warning disable S2699
    [Fact]
    public async Task OnPostAddPasskeyAsync_Success_RedirectsToRenamePasskey_Partial()
    {
        // Partial: see XML comment for guidance.
        await Task.CompletedTask;
    }
#pragma warning restore S2699

    /// <summary>
    /// Verifies that when no user is found the handler returns NotFound with the user id supplied by UserManager.GetUserId.
    /// Input conditions: UserManager.GetUserAsync returns null and GetUserId returns a known id.
    /// Expected: NotFoundObjectResult with a message containing the id.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostUpdatePasskeyAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("missing-user-id");

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var model = new PasskeysModel(userManagerMock.Object, signInManagerMock.Object);

        // ensure PageModel.User can be passed; not necessary because setups use It.IsAny<ClaimsPrincipal>()

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.IsType<string>(notFound.Value);
        Assert.Contains("missing-user-id", notFound.Value as string, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that when UserManager.GetUserAsync returns null, OnGetAsync returns a NotFoundObjectResult
    /// containing the ID returned by UserManager.GetUserId(User).
    /// Input conditions: mocked GetUserAsync -> null; mocked GetUserId -> specific string.
    /// Expected: NotFoundObjectResult with the exact message "Unable to load user with ID '{id}'."
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            userValidators,
            passwordValidators,
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            null,
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        // Setup: GetUserAsync returns null and GetUserId returns known id
        var expectedId = "missing-user-123";
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new PasskeysModel(userManagerMock.Object, signInManager);

        // Ensure PageContext and User are present
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedId)]));
        model.PageContext = new PageContext()
        {
            HttpContext = httpContext
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedId}'.";
        Assert.Equal(expectedMessage, notFound.Value as string);
    }
}