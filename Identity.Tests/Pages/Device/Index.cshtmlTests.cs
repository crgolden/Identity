#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Device;

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages.Device;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Device.IndexModel"/>.</summary>
[Trait("Category", "Unit")]
public class DeviceIndexModelTests
{
    /// <summary>
    /// Verifies that the IndexModel constructor does not throw when parameters are null.
    /// Inputs: all constructor parameters are null.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        IDeviceFlowInteractionService? interaction = null;
        IEventService? events = null;
        IOptions<IdentityServerOptions>? options = null;
        ILogger<IndexModel>? logger = null;

        // Act
        IndexModel model = null!;
        var ex = Record.Exception(() => model = new IndexModel(interaction, events, options, logger));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns a PageResult when userCode is null, because no
    /// device code lookup is needed — the user code entry form is shown.
    /// Inputs: userCode = null.
    /// Expected: result is PageResult.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_NullUserCode_ReturnsPage()
    {
        // Arrange
        var options = Options.Create(new IdentityServerOptions());
        var mockLogger = new Mock<ILogger<IndexModel>>(MockBehavior.Loose);
        var model = CreateModel(options: options, logger: mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns a PageResult and adds a model error when userCode is provided
    /// but the interaction service cannot resolve it (returns null).
    /// Inputs: userCode = "invalid-code", interaction.GetAuthorizationContextAsync returns null.
    /// Expected: result is PageResult and ModelState is invalid.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_InvalidUserCode_ReturnsPageWithModelError()
    {
        // Arrange
        var mockInteraction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAuthorizationContextAsync("invalid-code"))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);

        var options = Options.Create(new IdentityServerOptions());
        var mockLogger = new Mock<ILogger<IndexModel>>(MockBehavior.Loose);
        var model = CreateModel(mockInteraction.Object, options: options, logger: mockLogger.Object);

        // Act
        var result = await model.OnGetAsync("invalid-code");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    /// <summary>
    /// Verifies that OnPostAsync redirects to the error page when the authorization context
    /// cannot be resolved for Input.UserCode (interaction returns null).
    /// Inputs: Input.UserCode = "code", interaction.GetAuthorizationContextAsync returns null.
    /// Expected: result is RedirectToPageResult with page "/Error".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_NullAuthorizationContext_RedirectsToError()
    {
        // Arrange
        var mockInteraction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAuthorizationContextAsync("test-code"))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);

        var options = Options.Create(new IdentityServerOptions());
        var mockLogger = new Mock<ILogger<IndexModel>>(MockBehavior.Loose);
        var model = CreateModel(mockInteraction.Object, options: options, logger: mockLogger.Object);
        model.Input = new IndexModel.InputModel { UserCode = "test-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    private static IndexModel CreateModel(
        IDeviceFlowInteractionService? interaction = null,
        IEventService? events = null,
        IOptions<IdentityServerOptions>? options = null,
        ILogger<IndexModel>? logger = null)
    {
        options ??= Options.Create(new IdentityServerOptions());
        var model = new IndexModel(interaction, events, options, logger);
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
