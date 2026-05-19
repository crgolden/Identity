#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages;
using Infrastructure;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class CibaIndexModelTests
{
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        IBackchannelAuthenticationInteractionService? backchannelInteraction = null;

        // Act
        CibaModel model = null!;
        var ex = Record.Exception(() => model = new CibaModel(backchannelInteraction));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullId_RedirectsToError()
    {
        // Arrange
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        var model = CreateModel(mockService.Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_InvalidId_RedirectsToError()
    {
        // Arrange
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.GetLoginRequestByInternalIdAsync("invalid-id"))
            .ReturnsAsync((BackchannelUserLoginRequest?)null);

        var model = CreateModel(mockService.Object);

        // Act
        var result = await model.OnGetAsync("invalid-id");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidId_ReturnsPage()
    {
        // Arrange
        var loginRequest = new BackchannelUserLoginRequest();
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.GetLoginRequestByInternalIdAsync("valid-id"))
            .ReturnsAsync(loginRequest);

        var model = CreateModel(mockService.Object);

        // Act
        var result = await model.OnGetAsync("valid-id");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.LoginRequest);
    }

    private static CibaModel CreateModel(
        IBackchannelAuthenticationInteractionService? backchannelInteraction = null)
    {
        var model = new CibaModel(backchannelInteraction);
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
