namespace Identity.Tests.Unit.Pages.Account;
using Infrastructure;

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LogoutModelTests
{
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
        Assert.Null(model.SignOutIFrameUrl);
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
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnGetAsync_UnauthenticatedWithLogoutId_SetsContextProperties()
    {
        const string logoutId = "test-logout-id";
        var logoutRequest = new LogoutRequest(
            "https://signout.example.com/iframe",
            new LogoutMessage { PostLogoutRedirectUri = "https://client.example.com/signout-callback" });
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>())).ReturnsAsync(logoutRequest);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnGetAsync(logoutId);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowLogoutPrompt);
        Assert.Equal("https://client.example.com/signout-callback", model.PostLogoutRedirectUri);
        Assert.Equal("https://signout.example.com/iframe", model.SignOutIFrameUrl);
        interaction.Verify(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_WithLogoutId_SignsOutAndSetsContextProperties()
    {
        const string logoutId = "client-logout-id";
        var logoutRequest = new LogoutRequest(
            "https://signout.example.com/iframe",
            new LogoutMessage { PostLogoutRedirectUri = "https://client.example.com/signout-callback" });
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>())).ReturnsAsync(logoutRequest);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        var result = await model.OnPostAsync(logoutId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("https://client.example.com/signout-callback", model.PostLogoutRedirectUri);
        Assert.Equal("https://signout.example.com/iframe", model.SignOutIFrameUrl);
        interaction.Verify(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>()), Times.Once);
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
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        return new LogoutModel(
            signInManager.Object,
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
