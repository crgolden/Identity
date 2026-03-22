#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Trait("Category", "Unit")]
public class LogoutModelTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NullSignInManager_DoesNotThrow(bool loggerIsNull)
    {
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = loggerIsNull ? null : new Mock<ILogger<LogoutModel>>().Object;

        var exception = Record.Exception(() =>
        {
            var model = new LogoutModel(signInManager, logger, Mock.Of<IIdentityServerInteractionService>());
            Assert.NotNull(model);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task OnGetAsync_AuthenticatedUser_ShowsPromptWithoutCallingInteractionService()
    {
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAuthenticatedPageContext();

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowLogoutPrompt);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIframeUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnGetAsync_UnauthenticatedNoLogoutId_ReturnsPageWithoutCallingInteractionService()
    {
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowLogoutPrompt);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIframeUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnGetAsync_UnauthenticatedWithLogoutId_SetsContextProperties()
    {
        const string logoutId = "test-logout-id";
        var logoutRequest = new LogoutRequest(
            "https://signout.example.com/iframe",
            new LogoutMessage { PostLogoutRedirectUri = "https://client.example.com/signout-callback" });
        var interaction = new Mock<IIdentityServerInteractionService>();
        interaction.Setup(s => s.GetLogoutContextAsync(logoutId)).ReturnsAsync(logoutRequest);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnGetAsync(logoutId);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowLogoutPrompt);
        Assert.Equal("https://client.example.com/signout-callback", model.PostLogoutRedirectUri);
        Assert.Equal("https://signout.example.com/iframe", model.SignOutIframeUrl);
        interaction.Verify(s => s.GetLogoutContextAsync(logoutId), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_NoLogoutId_SignsOutAndReturnsPageWithoutCallingInteractionService()
    {
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIframeUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_WithLogoutId_SignsOutAndSetsContextProperties()
    {
        const string logoutId = "client-logout-id";
        var logoutRequest = new LogoutRequest(
            "https://signout.example.com/iframe",
            new LogoutMessage { PostLogoutRedirectUri = "https://client.example.com/signout-callback" });
        var interaction = new Mock<IIdentityServerInteractionService>();
        interaction.Setup(s => s.GetLogoutContextAsync(logoutId)).ReturnsAsync(logoutRequest);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnPostAsync(logoutId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("https://client.example.com/signout-callback", model.PostLogoutRedirectUri);
        Assert.Equal("https://signout.example.com/iframe", model.SignOutIframeUrl);
        interaction.Verify(s => s.GetLogoutContextAsync(logoutId), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnPostAsync_NullOrWhitespaceLogoutId_DoesNotCallInteractionService(string? logoutId)
    {
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnPostAsync(logoutId);

        Assert.IsType<PageResult>(result);
        interaction.VerifyNoOtherCalls();
    }

    private static LogoutModel BuildModel(IIdentityServerInteractionService? interactionService = null)
    {
        var userManager = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        var signInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null);
        return new LogoutModel(
            signInManager.Object,
            Mock.Of<ILogger<LogoutModel>>(),
            interactionService ?? Mock.Of<IIdentityServerInteractionService>());
    }

    private static PageContext BuildAuthenticatedPageContext()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "testuser")], "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        return new PageContext { HttpContext = httpContext };
    }

    private static PageContext BuildAnonymousPageContext()
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        return new PageContext { HttpContext = httpContext };
    }
}
