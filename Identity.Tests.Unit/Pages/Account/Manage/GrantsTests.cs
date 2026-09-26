namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class GrantsTests : IDisposable
{
    private static readonly string ExistingClientName = Generated.NewClientName();

    private static readonly string GrantedClientId = Generated.NewClientIdentifier();

    private static readonly string UnknownClientId = Generated.NewClientIdentifier();

    private static readonly string RevokedClientId = Generated.NewClientIdentifier();

    private static readonly string SignedInSubjectId = Generated.NewSubjectId();

    private static readonly string AuthenticationType = Generated.NewSchemeName();

    private static readonly string IdentityResourceDisplayName = Generated.NewDisplayName();

    private static readonly string ApiScopeDisplayName = Generated.NewDisplayName();

    private readonly TelemetryHarness _harness = new();

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

        var model = Create(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

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
        var claims = new[] { new Claim(JwtClaimTypes.Subject, SignedInSubjectId) };
        var identity = new ClaimsIdentity(claims, AuthenticationType);
        httpContext.User = new ClaimsPrincipal(identity);

        var model = Create(
            mockInteraction.Object,
            mockClients.Object,
            mockResources.Object,
            mockEvents.Object,
            httpContext);
        model.ClientId = RevokedClientId;

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Grants.GrantsPagePath, redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_WithGrants_ClientFound_PopulatesViewModelCorrectly()
    {
        // Arrange
        var grant = new Grant
        {
            ClientId = GrantedClientId,
            Scopes = [IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile],
            CreationTime = Generated.NewUtcDateTime(),
        };
        var client = new Client { ClientId = GrantedClientId, ClientName = ExistingClientName };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(GrantedClientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new IdentityResource { Name = IdentityServerConstants.StandardScopes.OpenId, DisplayName = IdentityResourceDisplayName }]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ApiScope { Name = IdentityServerConstants.StandardScopes.Profile, DisplayName = ApiScopeDisplayName }]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = Create(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        var grants = model.View.Grants.ToList();
        var onlyGrant = Assert.Single(grants);
        Assert.Equal(GrantedClientId, onlyGrant.ClientId);
        Assert.Equal(ExistingClientName, onlyGrant.ClientName);
        Assert.Contains(IdentityResourceDisplayName, onlyGrant.IdentityGrantNames);
        Assert.Contains(ApiScopeDisplayName, onlyGrant.ApiGrantNames);
    }

    [Fact]
    public async Task OnGetAsync_WithGrants_ClientNotFound_SkipsGrant()
    {
        // Arrange
        var grant = new Grant
        {
            ClientId = UnknownClientId,
            Scopes = [IdentityServerConstants.StandardScopes.OpenId],
            CreationTime = Generated.NewUtcDateTime(),
        };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(UnknownClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = Create(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Empty(model.View.Grants);
    }

    [Fact]
    public async Task OnGetAsync_MultipleGrants_OnlyClientFoundGrantsIncluded()
    {
        // Arrange
        var grant1 = new Grant
        {
            ClientId = GrantedClientId,
            Scopes = [IdentityServerConstants.StandardScopes.OpenId],
            CreationTime = Generated.NewUtcDateTime(),
        };
        var grant2 = new Grant
        {
            ClientId = UnknownClientId,
            Scopes = [IdentityServerConstants.StandardScopes.Profile],
            CreationTime = Generated.NewUtcDateTime(),
        };
        var client1 = new Client { ClientId = GrantedClientId, ClientName = Generated.NewClientName() };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant1, grant2]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(GrantedClientId, It.IsAny<CancellationToken>())).ReturnsAsync(client1);
        mockClients.Setup(x => x.FindClientByIdAsync(UnknownClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = Create(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        var onlyGrant = Assert.Single(model.View.Grants);
        Assert.Equal(GrantedClientId, onlyGrant.ClientId);
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
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtClaimTypes.Subject, SignedInSubjectId)], AuthenticationType));

        var model = Create(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object, httpContext);
        model.ClientId = RevokedClientId;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(RevokedClientId, revokedClientId);
    }

    public void Dispose() => _harness.Dispose();

    private Grants Create(
        IIdentityServerInteractionService interaction,
        IClientStore clients,
        IResourceStore resources,
        IEventService events,
        HttpContext? httpContext = null)
    {
        var model = new Grants(interaction, clients, resources, events, _harness.Telemetry);
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
