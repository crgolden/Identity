namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using System.Net;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DiagnosticsIndexModelTests
{
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Act
        var model = new DiagnosticsModel();

        // Assert
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_RemoteAddressNotLocal_ReturnsNotFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("8.8.8.8");

        var model = new DiagnosticsModel();
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = httpContext,
            RouteData = new RouteData(),
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_LocalhostRequest_ReturnsPage()
    {
        // Arrange
        var mockAuthService = new Mock<IAuthenticationService>(MockBehavior.Strict);
        mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string?>()))
            .ReturnsAsync(AuthenticateResult.NoResult());

        var services = new ServiceCollection();
        services.AddSingleton(mockAuthService.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        var model = new DiagnosticsModel();
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = httpContext,
            RouteData = new RouteData(),
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }
}
