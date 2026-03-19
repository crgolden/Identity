#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Trait("Category", "Unit")]
public class EmailModelTests
{
    public static TheoryData<string?> ValidEmailCases() => new()
    {
        "user@example.com",
        "user+tag@exa-mple.co.uk",
    };

    /// <summary>
    /// Tests that when no user is found (UserManager.GetUserAsync returns null),
    /// the handler returns a NotFoundObjectResult containing the user id from UserManager.GetUserId.
    /// Condition: UserManager.GetUserAsync returns null.
    /// Expected: NotFoundObjectResult with message referencing provided user id.
    /// </summary>
    [Fact]
    public async Task OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var emailSenderMock = new Mock<IEmailSender>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ignored")]));
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("expected-user-id");

        var model = new EmailModel(userManagerMock.Object, emailSenderMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        Assert.Contains("expected-user-id", notFound.Value.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Tests that when the ModelState is invalid the handler loads the page (returns PageResult).
    /// Condition: ModelState is invalid and a user exists.
    /// Expected: PageResult is returned and no email is sent.
    /// </summary>
    [Fact]
    public async Task OnPostSendVerificationEmailAsync_InvalidModelState_ReturnsPage()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var emailSenderMock = new Mock<IEmailSender>();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var model = new EmailModel(userManagerMock.Object, emailSenderMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Make ModelState invalid
        model.ModelState.AddModelError("Input.NewEmail", "Required");

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no email was attempted to be sent
        emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Tests that when a valid user exists and ModelState is valid the handler:
    /// - generates a token,
    /// - builds a callback URL via Url.Page,
    /// - sends an email with encoded callback URL,
    /// - sets the StatusMessage,
    /// - and redirects to the page.
    /// Condition: Valid user, various email values provided by MemberData.
    /// Expected: SendEmailAsync invoked with expected email/subject/body and RedirectToPageResult is returned.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidEmailCases))]
    public async Task OnPostSendVerificationEmailAsync_ValidUser_SendsEmailAndRedirects(string? returnedEmail)
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var emailSenderMock = new Mock<IEmailSender>();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("the-user-id");
        userManagerMock
            .Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(returnedEmail);
        userManagerMock
            .Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("raw-token");

        const string fixedCallbackUrl = "https://example.test/Account/ConfirmEmail?userId=the-user-id&code=abc";
        var capturedCallbackUrl = fixedCallbackUrl;
        var urlHelperMock = new Mock<IUrlHelper>();

        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Manage/Email";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        // SetReturnsDefault covers all string?-returning methods including RouteUrl called by Url.Page
        urlHelperMock.SetReturnsDefault<string?>(fixedCallbackUrl);

        string? capturedEmail = null;
        string? capturedSubject = null;
        string? capturedBody = null;

        emailSenderMock
            .Setup(es => es.SendEmailAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Callback<string?, string, string>((addr, subj, body) =>
            {
                capturedEmail = addr;
                capturedSubject = subj;
                capturedBody = body;
            });

        var model = new EmailModel(userManagerMock.Object, emailSenderMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            },
            Url = urlHelperMock.Object
        };

        // Ensure request scheme is set for Url.Page usage
        model.PageContext.HttpContext.Request.Scheme = "https";

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        // RedirectToPageResult expected
        Assert.IsType<RedirectToPageResult>(result);

        // StatusMessage should be set to expected value
        Assert.Equal("Verification email sent. Please check your email.", model.StatusMessage);

        // Verify email sender was invoked once
        emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

        // Subject should be the expected confirmation subject
        Assert.Equal("Confirm your email", capturedSubject);

        // The email address passed through should match returnedEmail (may be null)
        Assert.Equal(returnedEmail, capturedEmail);

        // The body should contain the encoded callback url
        Assert.NotNull(capturedBody);
        Assert.NotNull(capturedCallbackUrl);

        // Handler encodes callbackUrl via HtmlEncoder.Default.Encode
        var expectedEncodedUrl = HtmlEncoder.Default.Encode(capturedCallbackUrl);
        Assert.Contains(expectedEncodedUrl, capturedBody);
    }

    /// <summary>
    /// Verifies that the EmailModel constructor does not throw when provided with valid dependencies
    /// and that public properties are initialized to their expected defaults (null/false).
    /// Input conditions:
    /// - A real-ish UserManager and SignInManager are created via Moq with minimal required constructor arguments.
    /// - A mocked IEmailSender is provided.
    /// Expected result:
    /// - An instance is constructed successfully.
    /// - Email and StatusMessage are null, IsEmailConfirmed is false, and Input is null.
    /// </summary>
    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        var userManager = CreateUserManager();
        var emailSenderMock = new Mock<IEmailSender>();

        // Act
        var model = new EmailModel(userManager, emailSenderMock.Object);

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.Email);
        Assert.False(model.IsEmailConfirmed);
        Assert.Null(model.StatusMessage);
        Assert.NotNull(model.Input);
    }

    /// <summary>
    /// Ensures multiple instances constructed with different dependency instances are independent
    /// and all initialize to the same default public state.
    /// Input conditions:
    /// - Two distinct sets of dependencies (different mocks) are provided.
    /// Expected result:
    /// - Both instances are constructed without exception and expose identical default public property values.
    /// </summary>
    [Fact]
    public void Constructor_MultipleInstances_AreIndependent()
    {
        // Arrange - first instance
        var userManager1 = CreateUserManager();
        var emailSender1 = new Mock<IEmailSender>();

        // Arrange - second instance
        var userManager2 = CreateUserManager();
        var emailSender2 = new Mock<IEmailSender>();

        // Act
        var model1 = new EmailModel(userManager1, emailSender1.Object);
        var model2 = new EmailModel(userManager2, emailSender2.Object);

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);

        // Default public state assertions for both instances
        Assert.Null(model1.Email);
        Assert.False(model1.IsEmailConfirmed);
        Assert.Null(model1.StatusMessage);
        Assert.NotNull(model1.Input);

        Assert.Null(model2.Email);
        Assert.False(model2.IsEmailConfirmed);
        Assert.Null(model2.StatusMessage);
        Assert.NotNull(model2.Input);
    }

    /// <summary>
    /// Test that when the current user cannot be loaded (UserManager.GetUserAsync returns null),
    /// OnPostChangeEmailAsync returns a NotFoundObjectResult containing the user id returned by UserManager.GetUserId.
    /// </summary>
    [Fact]
    public async Task OnPostChangeEmailAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var hasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
                storeMock.Object,
                optionsMock.Object,
                hasherMock.Object,
                Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
                Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
                lookupNormalizerMock.Object,
                new IdentityErrorDescriber(),
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object)
        { CallBase = false };

        // Make GetUserAsync return null to simulate missing user.
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // Ensure GetUserId returns a specific id used in the NotFound message.
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("missing-user-id");

        var emailSenderMock = new Mock<IEmailSender>();

        var model = new EmailModel(userManagerMock.Object, emailSenderMock.Object)
        {
            // Ensure PageContext is available (PageModel may access Request, Url etc. but not needed here)
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Unable to load user with ID 'missing-user-id'", notFound.Value?.ToString() ?? string.Empty);
    }

    // Helper methods to create minimal mocks/instances needed for constructor invocation.
    // These helpers are local to the test class to avoid adding any extra types at namespace scope.
    private static UserManager<IdentityUser<Guid>> CreateUserManager()
    {
        // Minimal IUserStore needed for UserManager constructor
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();

        // Provide concrete/simple implementations for other parameters where feasible
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizer = Mock.Of<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        IServiceProvider? services = null;
        var logger = Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>();

        // Create a mock of UserManager using the required constructor arguments.
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            lookupNormalizer,
            errors,
            services,
            logger);

        return userManagerMock.Object;
    }
}