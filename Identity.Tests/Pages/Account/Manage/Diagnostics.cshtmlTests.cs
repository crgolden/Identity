#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;
using Identity.Tests.Infrastructure;

using System.Net;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Account.Manage.DiagnosticsModel"/>.</summary>
[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DiagnosticsIndexModelTests
{
    /// <summary>
    /// Verifies that DiagnosticsModel can be constructed without parameters and does not throw.
    /// Inputs: no constructor arguments.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Arrange & Act
        DiagnosticsModel model = null!;
        var ex = Record.Exception(() => model = new DiagnosticsModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns NotFoundResult when the remote IP address is not a
    /// local address (not 127.0.0.1 or ::1).
    /// Inputs: RemoteIpAddress = 8.8.8.8.
    /// Expected: result is NotFoundResult.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

    /// <summary>
    /// Verifies that OnGetAsync returns PageResult when the remote IP address is the loopback
    /// address (127.0.0.1) and authentication is available.
    /// Inputs: RemoteIpAddress = 127.0.0.1, auth service returns AuthenticateResult.NoResult().
    /// Expected: result is PageResult.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
