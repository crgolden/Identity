namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Net;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DiagnosticsTests
{
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Act
        var model = new Diagnostics();

        // Assert
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_RemoteAddressNotLocal_ReturnsNotFound()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = new IPAddress(Generated.NewIPv4AddressBytes());

        var model = new Diagnostics();
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

        var model = new Diagnostics();
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

    [Fact]
    public async Task OnGetAsync_IPv6LoopbackRequest_ReturnsPage()
    {
        // Arrange
        var model = ModelFor(IPAddress.IPv6Loopback, localAddress: null, AuthenticateResult.NoResult());

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_RequestFromTheServersOwnAddress_ReturnsPage()
    {
        // Arrange
        var serverAddress = new IPAddress(Generated.NewIPv4AddressBytes());
        var model = ModelFor(serverAddress, serverAddress, AuthenticateResult.NoResult());

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NoAuthenticatedPrincipal_ShowsNoClaimsOrTokens()
    {
        // Arrange
        var model = ModelFor(IPAddress.Loopback, localAddress: null, AuthenticateResult.NoResult());

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Empty(model.View.Claims);
        Assert.Empty(model.View.Tokens);
    }

    [Fact]
    public async Task OnGetAsync_AuthenticatedWithTokens_ShowsTheClaimsAndTokens()
    {
        // Arrange
        var claim = new System.Security.Claims.Claim(Generated.NewClaimType(), Generated.NewClaimValue());
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([claim], Generated.NewSchemeName()));
        var token = new AuthenticationToken { Name = Generated.NewPropertyKey(), Value = Generated.NewSessionKey() };
        var properties = new AuthenticationProperties();
        properties.StoreTokens([token]);
        var model = ModelFor(IPAddress.Loopback, localAddress: null, AuthenticateResult.Success(new AuthenticationTicket(principal, properties, Generated.NewSchemeName())));

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Contains(model.View.Claims, c => string.Equals(c.Type, claim.Type, StringComparison.Ordinal) && string.Equals(c.Value, claim.Value, StringComparison.Ordinal));
        var shown = Assert.Single(model.View.Tokens);
        Assert.Equal(token.Name, shown.Name);
        Assert.Equal(token.Value, shown.Value);
    }

    private static Diagnostics ModelFor(IPAddress remoteAddress, IPAddress? localAddress, AuthenticateResult authenticateResult)
    {
        var mockAuthService = new Mock<IAuthenticationService>(MockBehavior.Strict);
        mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string?>()))
            .ReturnsAsync(authenticateResult);
        var services = new ServiceCollection();
        services.AddSingleton(mockAuthService.Object);
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        httpContext.Connection.RemoteIpAddress = remoteAddress;
        httpContext.Connection.LocalIpAddress = localAddress;
        return new Diagnostics
        {
            PageContext = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor(),
                HttpContext = httpContext,
                RouteData = new RouteData(),
            },
        };
    }
}
