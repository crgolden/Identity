#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.CibaModel"/>.</summary>
[Trait("Category", "Unit")]
public class CibaIndexModelTests
{
    /// <summary>
    /// Verifies that the CibaModel constructor does not throw when all parameters are null.
    /// Inputs: backchannelInteraction = null, logger = null.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        IBackchannelAuthenticationInteractionService? backchannelInteraction = null;
        ILogger<CibaModel>? logger = null;

        // Act
        CibaModel model = null!;
        var ex = Record.Exception(() => model = new CibaModel(backchannelInteraction, logger));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGetAsync redirects to the error page when id is null.
    /// Inputs: id = null.
    /// Expected: result is RedirectToPageResult with page "/Error".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_NullId_RedirectsToError()
    {
        // Arrange
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        var mockLogger = new Mock<ILogger<CibaModel>>(MockBehavior.Loose);
        var model = CreateModel(mockService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    /// <summary>
    /// Verifies that OnGetAsync redirects to the error page when the backchannel service
    /// cannot resolve the given id (returns null).
    /// Inputs: id = "invalid-id", service returns null.
    /// Expected: result is RedirectToPageResult with page "/Error".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_InvalidId_RedirectsToError()
    {
        // Arrange
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.GetLoginRequestByInternalIdAsync("invalid-id"))
            .ReturnsAsync((BackchannelUserLoginRequest?)null);

        var mockLogger = new Mock<ILogger<CibaModel>>(MockBehavior.Loose);
        var model = CreateModel(mockService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync("invalid-id");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns PageResult and sets LoginRequest when the backchannel
    /// service resolves a valid id.
    /// Inputs: id = "valid-id", service returns a BackchannelUserLoginRequest.
    /// Expected: result is PageResult and model.LoginRequest is not null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_ValidId_ReturnsPage()
    {
        // Arrange
        var loginRequest = new BackchannelUserLoginRequest();
        var mockService = new Mock<IBackchannelAuthenticationInteractionService>(MockBehavior.Strict);
        mockService
            .Setup(x => x.GetLoginRequestByInternalIdAsync("valid-id"))
            .ReturnsAsync(loginRequest);

        var mockLogger = new Mock<ILogger<CibaModel>>(MockBehavior.Loose);
        var model = CreateModel(mockService.Object, mockLogger.Object);

        // Act
        var result = await model.OnGetAsync("valid-id");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.LoginRequest);
    }

    private static CibaModel CreateModel(
        IBackchannelAuthenticationInteractionService? backchannelInteraction = null,
        ILogger<CibaModel>? logger = null)
    {
        var model = new CibaModel(backchannelInteraction, logger);
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
