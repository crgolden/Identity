namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Duende.IdentityServer.Services;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ServerSideSessionsIndexModelTests
{
    [Fact]
    public void Constructor_NullService_DoesNotThrow()
    {
        // Act
        var model = new ServerSideSessionsModel();

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Constructor_NoService_DefaultConstructor_DoesNotThrow()
    {
        // Act
        var model = new ServerSideSessionsModel();

        // Assert
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullService_UserSessionsRemainsNull()
    {
        // Arrange
        var model = CreateModel(null);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Null(model.UserSessions);
    }

    [Fact]
    public async Task OnPostAsync_NullService_RedirectsToPage()
    {
        // Arrange
        var model = CreateModel(null);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Manage/ServerSideSessions", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_WithService_RemovesSessionAndRedirects()
    {
        // Arrange
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(mockService.Object);
        model.SessionId = "session-abc";

        // Act
        var result = await model.OnPostAsync();

        // Assert
        mockService.Verify(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>(), It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Manage/ServerSideSessions", redirect.PageName);
    }

    private static ServerSideSessionsModel CreateModel(ISessionManagementService? sessionManagement)
    {
        var model = new ServerSideSessionsModel(sessionManagement);
        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = httpContext,
            RouteData = new RouteData(),
        };
        return model;
    }
}
