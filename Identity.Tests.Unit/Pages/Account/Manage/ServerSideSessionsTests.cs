namespace Identity.Tests.Unit.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ServerSideSessionsTests
{
    [Fact]
    public void Constructor_NullService_DoesNotThrow()
    {
        // Act
        var model = new ServerSideSessions();

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Constructor_NoService_DefaultConstructor_DoesNotThrow()
    {
        // Act
        var model = new ServerSideSessions();

        // Assert
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullService_UserSessionsRemainsNull()
    {
        // Arrange
        var model = Create(null);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Null(model.UserSessions);
    }

    [Fact]
    public async Task OnPostAsync_NullService_RedirectsToPage()
    {
        // Arrange
        var model = Create(null);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ServerSideSessions.ServerSideSessionsPagePath, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_WithService_RemovesSessionAndRedirects()
    {
        // Arrange
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var model = Create(mockService.Object);
        model.SessionId = Generated.NewSessionKey();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        mockService.Verify(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>(), It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ServerSideSessions.ServerSideSessionsPagePath, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_WithService_RemovesExactlyThePostedSession()
    {
        // Arrange
        var removed = new List<RemoveSessionsContext>();
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.RemoveSessionsAsync(It.IsAny<RemoveSessionsContext>(), It.IsAny<CancellationToken>()))
            .Callback<RemoveSessionsContext, CancellationToken>((context, _) => removed.Add(context))
            .Returns(Task.CompletedTask);
        var model = Create(mockService.Object);
        model.SessionId = Generated.NewSessionKey();

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(model.SessionId, Assert.Single(removed).SessionId);
    }

    [Fact]
    public async Task OnGetAsync_WithService_QueriesWithEveryFilterAndThePriorResultsFlag()
    {
        // Arrange
        var queries = new List<SessionQuery>();
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.QuerySessionsAsync(It.IsAny<SessionQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SessionQuery, CancellationToken>((query, _) => queries.Add(query))
            .ReturnsAsync(new QueryResult<UserSession>());
        var model = Create(mockService.Object);
        model.Token = Generated.NewSessionKey();
        model.Prev = ServerSideSessions.RequestPriorResultsValue;
        model.DisplayNameFilter = Generated.NewDisplayName();
        model.SessionIdFilter = Generated.NewSessionKey();
        model.SubjectIdFilter = Generated.NewSubjectId();

        // Act
        await model.OnGetAsync();

        // Assert
        var query = Assert.Single(queries);
        Assert.Equal(model.Token, query.ResultsToken);
        Assert.True(query.RequestPriorResults);
        Assert.Equal(model.DisplayNameFilter, query.DisplayName);
        Assert.Equal(model.SessionIdFilter, query.SessionId);
        Assert.Equal(model.SubjectIdFilter, query.SubjectId);
        Assert.NotNull(model.UserSessions);
    }

    [Fact]
    public async Task OnGetAsync_WithServiceAndAnyOtherPrevValue_DoesNotRequestPriorResults()
    {
        // Arrange
        var queries = new List<SessionQuery>();
        var mockService = new Mock<ISessionManagementService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.QuerySessionsAsync(It.IsAny<SessionQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SessionQuery, CancellationToken>((query, _) => queries.Add(query))
            .ReturnsAsync(new QueryResult<UserSession>());
        var model = Create(mockService.Object);
        model.Prev = Generated.NewTokenOtherThan(ServerSideSessions.RequestPriorResultsValue);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.False(Assert.Single(queries).RequestPriorResults);
    }

    private static ServerSideSessions Create(ISessionManagementService? sessionManagement)
    {
        var model = new ServerSideSessions(sessionManagement);
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
