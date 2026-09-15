namespace Identity.Tests.Unit.Pages.Account;

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LogoutModelTests
{
    private static readonly string SignOutIFrameUrl = TestValues.NewCallbackUrl();

    private static readonly string PostLogoutRedirectUri = TestValues.NewCallbackUrl();

    public static TheoryData<string?> BlankLogoutIds() => new()
    {
        (string?)null,
        string.Empty,
        TestValues.NewWhitespaceValue(),
    };

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
        var logoutId = TestValues.NewLogoutId();
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
        Assert.Null(redirect.RouteValues?[LogoutModel.LogoutIdRouteValueName]);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_WithLogoutId_SignsOutAndRedirectsToSelfWithLogoutId()
    {
        // Arrange
        var logoutId = TestValues.NewLogoutId();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync(logoutId);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(logoutId, redirect.RouteValues?[LogoutModel.LogoutIdRouteValueName]);
        Assert.Null(model.PostLogoutRedirectUri);
        Assert.Null(model.SignOutIFrameUrl);
        interaction.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(BlankLogoutIds))]
    public async Task OnPostAsync_NullOrWhitespaceLogoutId_DoesNotCallInteractionService(string? logoutId)
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = BuildModel(interaction.Object);
        model.PageContext = BuildAnonymousPageContext();

        // Act
        var result = await model.OnPostAsync(logoutId);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
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
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, TestValues.NewUserName())], TestValues.NewSchemeName());
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