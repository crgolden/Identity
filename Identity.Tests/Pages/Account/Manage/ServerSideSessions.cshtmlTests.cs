#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;
using Identity.Tests.Infrastructure;

using Duende.IdentityServer.Services;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Account.Manage.ServerSideSessionsModel"/>.</summary>
[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ServerSideSessionsIndexModelTests
{
    /// <summary>
    /// Verifies that the ServerSideSessionsModel constructor does not throw when the optional service is null.
    /// Inputs: sessionManagement = null.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NullService_DoesNotThrow()
    {
        // Arrange & Act
        ServerSideSessionsModel model = null!;
        var ex = Record.Exception(() => model = new ServerSideSessionsModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies that the ServerSideSessionsModel default constructor (no arguments) does not throw.
    /// Inputs: no arguments (uses optional parameter default of null).
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NoService_DefaultConstructor_DoesNotThrow()
    {
        // Arrange & Act
        ServerSideSessionsModel model = null!;
        var ex = Record.Exception(() => model = new ServerSideSessionsModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGetAsync with a null session management service leaves UserSessions as null.
    /// Inputs: sessionManagement = null.
    /// Expected: model.UserSessions is null after OnGetAsync.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

    /// <summary>
    /// Verifies that OnPostAsync with a null session management service still redirects to the page.
    /// Inputs: sessionManagement = null.
    /// Expected: result is RedirectToPageResult pointing to "/Account/Manage/ServerSideSessions".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

    /// <summary>
    /// Verifies that OnPostAsync with a live session management service calls RemoveSessionsAsync
    /// and then redirects to the page.
    /// Inputs: mock service that accepts RemoveSessionsAsync, SessionId = "session-abc".
    /// Expected: RemoveSessionsAsync is called once and result is RedirectToPageResult.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_WithService_RemovesSessionAndRedirects()
    {
        // Arrange
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>()))
            .Returns(Task.CompletedTask);

        var model = CreateModel(mockService.Object);
        model.SessionId = "session-abc";

        // Act
        var result = await model.OnPostAsync();

        // Assert
        mockService.Verify(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>()), Times.Once);
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
