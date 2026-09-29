namespace Identity.Tests.Unit.Pages.Account;

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LogoutTests
{
    private static readonly string SignOutIFrameUrl = Generated.NewCallbackAddress();

    private static readonly string PostLogoutRedirectUri = Generated.NewCallbackAddress();

    [Fact]
    public async Task OnGetAsync_AuthenticatedUser_ShowsPromptWithoutCallingInteractionService()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAuthenticatedPageContext();

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.ShowLogoutPrompt);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnGetAsync_UnauthenticatedNoLogoutId_ReturnsPageWithoutCallingInteractionService()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowLogoutPrompt);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnGetAsync_UnauthenticatedWithLogoutId_SetsContextProperties()
    {
        // Arrange
        var logoutId = Generated.NewLogoutId();
        var logoutRequest = new LogoutRequest(
            SignOutIFrameUrl,
            new LogoutMessage { PostLogoutRedirectUri = PostLogoutRedirectUri });
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>())).ReturnsAsync(logoutRequest);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnGetAsync(logoutId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ShowLogoutPrompt);
        Assert.Equal(PostLogoutRedirectUri, model.PostLogoutRedirectUri);
        Assert.Equal(SignOutIFrameUrl, model.SignOutIFrameUrl);
        interaction.Verify(s => s.GetLogoutContextAsync(logoutId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_NoLogoutId_SignsOutAndRedirectsWithoutCallingInteractionService()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.RouteValues?[Logout.LogoutIdRouteValueName]);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_WithLogoutId_SignsOutAndRedirectsToSelfWithLogoutId()
    {
        // Arrange
        var logoutId = Generated.NewLogoutId();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync(logoutId);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(logoutId, redirect.RouteValues?[Logout.LogoutIdRouteValueName]);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_EmptyLogoutId_DoesNotCallInteractionService()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync(Generated.NewBlank());

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_WhitespaceLogoutId_DoesNotCallInteractionService()
    {
        // Arrange
        var whitespaceLogoutId = Generated.NewWhitespaceValue();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync(whitespaceLogoutId);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_SignsTheUserOut()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);
        var model = new Logout(signInManager.Object, Mock.Of<IIdentityServerInteractionService>());
        model.PageContext = BuildAnonymousPageContext();
        var logoutId = Generated.NewLogoutId();

        // Act
        await model.OnPostAsync(logoutId);

        // Assert
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    private static Logout BuildModel(IIdentityServerInteractionService? interactionService = null)
    {
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        return new Logout(
            signInManager.Object,
            interactionService ?? Mock.Of<IIdentityServerInteractionService>());
    }

    private static PageContext BuildAuthenticatedPageContext()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, Generated.NewUserName())], Generated.NewSchemeName());
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
