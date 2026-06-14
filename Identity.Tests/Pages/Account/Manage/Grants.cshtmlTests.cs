#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

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

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GrantsIndexModelTests
{
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

    [Fact]
    public async Task OnGetAsync_NoGrants_SetsEmptyViewModel()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task OnPostAsync_RevokesGrant_RedirectsToPage()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.RevokeUserConsentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        mockEvents
            .Setup(x => x.RaiseAsync(It.IsAny<GrantsRevokedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim("sub", "user1") };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

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

    [Fact]
    public async Task OnGetAsync_WithGrants_ClientFound_PopulatesViewModelCorrectly()
    {
        // Arrange
        var grant = new Grant { ClientId = "c1", Scopes = ["openid", "profile"], CreationTime = DateTime.UtcNow };
        var client = new Client { ClientId = "c1", ClientName = "My App" };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(client);

        // FindResourcesByScopeAsync is an extension method; mock the three underlying interface methods it calls.
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<IdentityResource>)[new IdentityResource { Name = "openid", DisplayName = "Your user identifier" }]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ApiResource>)[]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task OnGetAsync_WithGrants_ClientNotFound_SkipsGrant()
    {
        // Arrange
        var grant = new Grant { ClientId = "missing-client", Scopes = ["openid"], CreationTime = DateTime.UtcNow };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("missing-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Empty(model.View.Grants);
    }

    [Fact]
    public async Task OnGetAsync_MultipleGrants_OnlyClientFoundGrantsIncluded()
    {
        // Arrange
        var grant1 = new Grant { ClientId = "c1", Scopes = ["openid"], CreationTime = DateTime.UtcNow };
        var grant2 = new Grant { ClientId = "c2-missing", Scopes = ["profile"], CreationTime = DateTime.UtcNow };
        var client1 = new Client { ClientId = "c1", ClientName = "Client One" };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant1, grant2]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync("c1", It.IsAny<CancellationToken>())).ReturnsAsync(client1);
        mockClients.Setup(x => x.FindClientByIdAsync("c2-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        // FindResourcesByScopeAsync is an extension method; mock the three underlying interface methods it calls.
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<IdentityResource>)[]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ApiResource>)[]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ApiScope>)[]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Single(model.View.Grants);
        Assert.Equal("c1", model.View.Grants.First().ClientId);
    }

    [Fact]
    public async Task OnPostAsync_PassesClientIdToRevoke()
    {
        // Arrange
        string? revokedClientId = null;

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(x => x.RevokeUserConsentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => revokedClientId = id)
            .Returns(Task.CompletedTask);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        mockEvents.Setup(x => x.RaiseAsync(It.IsAny<GrantsRevokedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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
