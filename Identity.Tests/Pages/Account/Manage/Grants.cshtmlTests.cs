#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Account.Manage.GrantsModel"/>.</summary>
[Trait("Category", "Unit")]
public class GrantsIndexModelTests
{
    /// <summary>
    /// Verifies that the GrantsModel constructor does not throw when all parameters are null.
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
        GrantsModel model = null!;
        var ex = Record.Exception(() => model = new GrantsModel(interaction, clients, resources, events));

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
    /// Verifies that OnPostAsync revokes the user's grant and redirects to the Grants page.
    /// Inputs: ClientId bound on model, interaction.RevokeUserConsentAsync and events.RaiseAsync are invoked.
    /// Expected: result is RedirectToPageResult pointing to "/Account/Manage/Grants".
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
        Assert.Equal("/Account/Manage/Grants", redirect.PageName);
    }

    /// <summary>
    /// Verifies that OnGetAsync populates View.Grants with correct data when a client is found for the grant.
    /// Input: one grant with ClientId="c1"; client found with name "My App"; resources contain one identity and one API scope.
    /// Expected: View.Grants has exactly one entry with matching ClientId, ClientName, IdentityGrantNames, and ApiGrantNames.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_WithGrants_ClientFound_PopulatesViewModelCorrectly()
    {
        // Arrange
        var grant = new Grant { ClientId = "c1", Scopes = ["openid", "profile"], CreationTime = DateTime.UtcNow };
        var client = new Duende.IdentityServer.Models.Client { ClientId = "c1", ClientName = "My App" };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync()).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("c1")).ReturnsAsync(client);

        // FindResourcesByScopeAsync is an extension method; mock the three underlying interface methods it calls.
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<IdentityResource>)[new IdentityResource { Name = "openid", DisplayName = "Your user identifier" }]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<ApiResource>)[]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<ApiScope>)[new ApiScope { Name = "profile", DisplayName = "Profile" }]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        var grants = model.View.Grants.ToList();
        Assert.Single(grants);
        Assert.Equal("c1", grants[0].ClientId);
        Assert.Equal("My App", grants[0].ClientName);
        Assert.Contains("Your user identifier", grants[0].IdentityGrantNames);
        Assert.Contains("Profile", grants[0].ApiGrantNames);
    }

    /// <summary>
    /// Verifies that OnGetAsync skips grants where FindClientByIdAsync returns null.
    /// Input: one grant; FindClientByIdAsync returns null.
    /// Expected: View.Grants is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_WithGrants_ClientNotFound_SkipsGrant()
    {
        // Arrange
        var grant = new Grant { ClientId = "missing-client", Scopes = ["openid"], CreationTime = DateTime.UtcNow };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync()).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("missing-client"))
            .ReturnsAsync((Duende.IdentityServer.Models.Client?)null);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Empty(model.View.Grants);
    }

    /// <summary>
    /// Verifies that OnGetAsync only includes grants where a client is found, skipping those where it is null.
    /// Input: two grants; first has a client, second does not.
    /// Expected: View.Grants has exactly one entry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_MultipleGrants_OnlyClientFoundGrantsIncluded()
    {
        // Arrange
        var grant1 = new Grant { ClientId = "c1", Scopes = ["openid"], CreationTime = DateTime.UtcNow };
        var grant2 = new Grant { ClientId = "c2-missing", Scopes = ["profile"], CreationTime = DateTime.UtcNow };
        var client1 = new Duende.IdentityServer.Models.Client { ClientId = "c1", ClientName = "Client One" };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync()).ReturnsAsync([grant1, grant2]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("c1")).ReturnsAsync(client1);
        mockClients.Setup(x => x.FindClientByIdAsync("c2-missing"))
            .ReturnsAsync((Duende.IdentityServer.Models.Client?)null);

        // FindResourcesByScopeAsync is an extension method; mock the three underlying interface methods it calls.
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<IdentityResource>)[]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<ApiResource>)[]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IReadOnlyCollection<ApiScope>)[]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Single(model.View.Grants);
        Assert.Equal("c1", model.View.Grants.First().ClientId);
    }

    /// <summary>
    /// Verifies that OnPostAsync calls RevokeUserConsentAsync with the exact ClientId bound on the model.
    /// Input: ClientId = "client1".
    /// Expected: RevokeUserConsentAsync is called with "client1".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_PassesClientIdToRevoke()
    {
        // Arrange
        string? revokedClientId = null;

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.RevokeUserConsentAsync(It.IsAny<string>()))
            .Callback<string>(id => revokedClientId = id)
            .Returns(Task.CompletedTask);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        mockEvents.Setup(x => x.RaiseAsync(It.IsAny<GrantsRevokedEvent>())).Returns(Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user1")], "test"));

        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object, httpContext);
        model.ClientId = "client1";

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal("client1", revokedClientId);
    }

    private static GrantsModel CreateModel(
        IIdentityServerInteractionService? interaction = null,
        IClientStore? clients = null,
        IResourceStore? resources = null,
        IEventService? events = null,
        HttpContext? httpContext = null)
    {
        var model = new GrantsModel(interaction, clients, resources, events);
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
