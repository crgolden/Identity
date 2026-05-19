#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeviceIndexModelTests
{
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        IDeviceFlowInteractionService? interaction = null;
        IEventService? events = null;

        // Act
        DeviceModel model = null!;
        var ex = Record.Exception(() => model = new DeviceModel(interaction, events));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullUserCode_ReturnsPage()
    {
        // Arrange
        var model = CreateModel();

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_InvalidUserCode_ReturnsPageWithModelError()
    {
        // Arrange
        var mockInteraction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAuthorizationContextAsync("invalid-code"))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);

        var model = CreateModel(mockInteraction.Object);

        // Act
        var result = await model.OnGetAsync("invalid-code");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_NullAuthorizationContext_RedirectsToError()
    {
        // Arrange
        var mockInteraction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAuthorizationContextAsync("test-code"))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);

        var model = CreateModel(mockInteraction.Object);
        model.Input = new DeviceModel.InputModel { UserCode = "test-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    private static DeviceModel CreateModel(
        IDeviceFlowInteractionService? interaction = null,
        IEventService? events = null)
    {
        var model = new DeviceModel(interaction, events);
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
