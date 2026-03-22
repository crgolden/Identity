#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Grants;

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Pages.Grants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Grants.IndexModel"/>.</summary>
[Trait("Category", "Unit")]
public class GrantsIndexModelTests
{
    /// <summary>
    /// Verifies that the IndexModel constructor does not throw when all parameters are null.
    /// Inputs: all four constructor parameters are null.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        IIdentityServerInteractionService? interaction = null;
        IClientStore? clients = null;
        IResourceStore? resources = null;
        IEventService? events = null;

        // Act
        IndexModel model = null!;
        var ex = Record.Exception(() => model = new IndexModel(interaction, clients, resources, events));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGetAsync with no grants results in an empty Grants list on the view model.
    /// Inputs: interaction.GetAllUserGrantsAsync returns an empty list.
    /// Expected: model.View.Grants is empty after OnGetAsync completes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_NoGrants_SetsEmptyViewModel()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAllUserGrantsAsync())
            .ReturnsAsync([]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);

        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.NotNull(model.View);
        Assert.Empty(model.View.Grants);
    }

    /// <summary>
    /// Verifies that OnPostAsync revokes the user's grant and redirects to the Grants Index page.
    /// Inputs: ClientId bound on model, interaction.RevokeUserConsentAsync and events.RaiseAsync are invoked.
    /// Expected: result is RedirectToPageResult pointing to "/Grants/Index".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_RevokesGrant_RedirectsToPage()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.RevokeUserConsentAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        mockEvents
            .Setup(x => x.RaiseAsync(It.IsAny<GrantsRevokedEvent>()))
            .Returns(Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new System.Security.Claims.Claim("sub", "user1") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "test");
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);

        var model = CreateModel(
            mockInteraction.Object,
            mockClients.Object,
            mockResources.Object,
            mockEvents.Object,
            httpContext);
        model.ClientId = "client1";

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Grants/Index", redirect.PageName);
    }

    private static IndexModel CreateModel(
        IIdentityServerInteractionService? interaction = null,
        IClientStore? clients = null,
        IResourceStore? resources = null,
        IEventService? events = null,
        HttpContext? httpContext = null)
    {
        var model = new IndexModel(interaction, clients, resources, events);
        var ctx = httpContext ?? new DefaultHttpContext();
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = ctx,
            RouteData = new RouteData(),
        };
        return model;
    }
}
