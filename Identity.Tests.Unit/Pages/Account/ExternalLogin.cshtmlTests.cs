namespace Identity.Tests.Unit.Pages.Account;

using System.Security.Claims;
using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ExternalLoginModelTests
{
    [Fact]
    public void OnGet_RedirectsToLoginPage()
    {
        // Arrange
        var harness = CreateModel();

        // Act
        var result = harness.Model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
    }

    [Fact]
    public void OnPost_ReturnsChallengeResultForProvider()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn
            .Setup(s => s.ConfigureExternalAuthenticationProperties("Google", It.IsAny<string?>(), null))
            .Returns(new AuthenticationProperties());

        // Act
        var result = harness.Model.OnPost("Google", "/return");

        // Assert
        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains("Google", challenge.AuthenticationSchemes);
    }

    [Fact]
    public async Task OnGetCallbackAsync_RemoteError_RedirectsToLogin()
    {
        // Arrange
        var harness = CreateModel();

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/return", "provider failure");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.Contains("provider failure", harness.Model.ErrorMessage, StringComparison.Ordinal);
        harness.SignIn.Verify(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task OnGetCallbackAsync_InfoNull_RedirectsToLogin()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync((ExternalLoginInfo?)null);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/return", null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.Equal("Error loading external login information.", harness.Model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetCallbackAsync_SignInSucceeds_LocalReturnUrl_LocalRedirects()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/local", redirect.Url);
    }

    [Fact]
    public async Task OnGetCallbackAsync_SignInSucceeds_NonLocalReturnUrl_RedirectsToRoot()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("http://evil.example", null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
    }

    [Fact]
    public async Task OnGetCallbackAsync_LockedOut_RedirectsToLockout()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirect.PageName);
    }

    [Fact]
    public async Task OnGetCallbackAsync_WithEmailClaim_NoExistingUser_CreatesAccountAndSignsIn()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo("found@example.com"));
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        harness.SignIn.Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        harness.UserMgr.Setup(u => u.FindByEmailAsync("found@example.com")).ReturnsAsync((IdentityUser<Guid>?)null);
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/local", redirect.Url);
        harness.UserMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>()), Times.Once);
        harness.UserMgr.Verify(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>()), Times.Once);
        harness.SignIn.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, "Google"), Times.Once);
        Assert.Null(harness.Model.Input.Email);
    }

    [Fact]
    public async Task OnGetCallbackAsync_EmailVerifiedClaimTrue_SkipsConfirmationEmailAndSignsInImmediately()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: true);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo("found@example.com", emailVerified: true));
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        harness.SignIn.Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        harness.UserMgr.Setup(u => u.FindByEmailAsync("found@example.com")).ReturnsAsync((IdentityUser<Guid>?)null);
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.IsEmailConfirmedAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(true);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/local", redirect.Url);
        harness.EmailStore.Verify(s => s.SetEmailConfirmedAsync(It.IsAny<IdentityUser<Guid>>(), true, It.IsAny<CancellationToken>()), Times.Once);
        harness.Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.SignIn.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, "Google"), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_EmailVerifiedClaimFalse_DoesNotConfirmEmail()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: true);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo("found@example.com", emailVerified: false));
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        harness.UserMgr.Setup(u => u.FindByEmailAsync("found@example.com")).ReturnsAsync((IdentityUser<Guid>?)null);
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        harness.EmailStore.Verify(s => s.SetEmailConfirmedAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        harness.Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetCallbackAsync_WithEmailClaim_ExistingUser_RedirectsToLoginWithoutCreatingAccount()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo("found@example.com"));
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        harness.UserMgr.Setup(u => u.FindByEmailAsync("found@example.com")).ReturnsAsync(new IdentityUser<Guid> { Email = "found@example.com" });

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.Contains("found@example.com", harness.Model.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("Display", harness.Model.ErrorMessage, StringComparison.Ordinal);
        harness.UserMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnGetCallbackAsync_RequiresRegistration_NoEmailClaim_ReturnsPageWithoutInputEmail()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo(email: null));
        harness.SignIn.Setup(s => s.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await harness.Model.OnGetCallbackAsync("/local", null);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Null(harness.Model.Input.Email);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_InfoNull_RedirectsToLogin()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync((ExternalLoginInfo?)null);

        // Act
        var result = await harness.Model.OnPostConfirmationAsync(null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.Equal("Error loading external login information during confirmation.", harness.Model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.Model.ModelState.AddModelError("Test", "error");
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Display", harness.Model.ProviderDisplayName);
        Assert.Equal("/return", harness.Model.ReturnUrl);
        harness.UserMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_ExistingUser_RedirectsToLoginWithoutCreatingAccount()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.UserMgr.Setup(u => u.FindByEmailAsync("user@example.com")).ReturnsAsync(new IdentityUser<Guid> { Email = "user@example.com" });
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/return");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.Contains("user@example.com", harness.Model.ErrorMessage, StringComparison.Ordinal);
        harness.UserMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_EmailBlank_ReturnsPage()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = null };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        harness.UserMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateFails_AddsErrorsAndReturnsPage()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "create failed" }));
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(harness.Model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateSucceeds_AddLoginFails_ReturnsPage()
    {
        // Arrange
        var harness = CreateModel();
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "add login failed" }));
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        harness.Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateSucceeds_RequireConfirmed_SendsEmailAndRedirectsToRegisterConfirmation()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: true);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/local");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./RegisterConfirmation", redirect.PageName);
        Assert.Equal("user@example.com", redirect.RouteValues?["email"]);
        harness.Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.SignIn.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateSucceeds_NoConfirm_LocalReturn_SignsInAndLocalRedirects()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: false);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.SignIn.Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/local");

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/local", redirect.Url);
        harness.SignIn.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, "Google"), Times.Once);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateSucceeds_NoConfirm_NonLocalReturn_RedirectsToRoot()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: false);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.SignIn.Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("http://evil.example");

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
    }

    [Fact]
    public async Task OnPostConfirmationAsync_CreateSucceeds_CallbackUrlNull_SkipsEmailAndStillRedirects()
    {
        // Arrange
        var harness = CreateModel(requireConfirmedAccount: true);
        harness.Url.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns((string?)null);
        harness.SignIn.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>())).ReturnsAsync(BuildLoginInfo());
        harness.UserMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(IdentityResult.Success);
        harness.UserMgr.Setup(m => m.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        harness.Model.Input = new ExternalLoginModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await harness.Model.OnPostConfirmationAsync("/local");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./RegisterConfirmation", redirect.PageName);
        harness.Sender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ExternalLoginInfo BuildLoginInfo(string? email = "user@example.com", bool? emailVerified = null)
    {
        var claims = new List<Claim>();
        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (emailVerified.HasValue)
        {
            claims.Add(new Claim("email_verified", emailVerified.Value ? "true" : "false"));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return new ExternalLoginInfo(principal, "Google", "provider-key", "Display");
    }

    private static (IAzureClientFactory<ServiceBusClient> Factory, Mock<ServiceBusSender> Sender) CreateServiceBusFactory()
    {
        var sender = new Mock<ServiceBusSender>(MockBehavior.Strict);
        sender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var client = new Mock<ServiceBusClient>(MockBehavior.Strict);
        client.Setup(c => c.CreateSender("email")).Returns(sender.Object);
        var factory = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient("crgolden")).Returns(client.Object);
        return (factory.Object, sender);
    }

    private static (ExternalLoginModel Model, Mock<SignInManager<IdentityUser<Guid>>> SignIn, Mock<UserManager<IdentityUser<Guid>>> UserMgr, Mock<ServiceBusSender> Sender, Mock<IUrlHelper> Url, Mock<IUserEmailStore<IdentityUser<Guid>>> EmailStore) CreateModel(bool requireConfirmedAccount = false)
    {
        var emailStore = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
        emailStore.Setup(s => s.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        emailStore.Setup(s => s.SetEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        emailStore.Setup(s => s.SetEmailConfirmedAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var store = emailStore.As<IUserStore<IdentityUser<Guid>>>();

        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        var options = Options.Create(identityOptions);

        var userManager = new Mock<UserManager<IdentityUser<Guid>>>(
            store.Object,
            options,
            new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object,
            Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);
        userManager.SetupGet(u => u.SupportsUserEmail).Returns(true);
        userManager.Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync("the-user-id");
        userManager.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync("email-token");
        userManager.Setup(u => u.GetClaimsAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(new List<Claim>());
        userManager.Setup(u => u.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser<Guid>?)null);

        var signIn = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object,
            options,
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object);

        var (factory, sender) = CreateServiceBusFactory();

        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Content("~/")).Returns("/");
        url.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns<string?>(u => u is not null && u.StartsWith('/') && !u.StartsWith("//", StringComparison.Ordinal));
        var routeData = new RouteData();
        routeData.Values["page"] = "/Account/ExternalLogin"; // needed so relative page paths (./ExternalLogin) resolve
        url.SetupGet(u => u.ActionContext).Returns(new ActionContext(new DefaultHttpContext(), routeData, new ActionDescriptor()));
        url.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example/confirm");

        var model = new ExternalLoginModel(signIn.Object, userManager.Object, store.Object, factory)
        {
            Url = url.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
        };

        return (model, signIn, userManager, sender, url, emailStore);
    }
}
