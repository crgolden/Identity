namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GrantsIndexModelTests
{
    private static readonly string ExistingClientName = TestValues.NewClientName();

    private static readonly string GrantedClientId = TestValues.NewClientIdentifier();

    private static readonly string UnknownClientId = TestValues.NewClientIdentifier();

    private static readonly string RevokedClientId = TestValues.NewClientIdentifier();

    private static readonly string SignedInSubjectId = TestValues.NewSubjectId();

    private static readonly string AuthenticationType = TestValues.NewSchemeName();

    private static readonly string IdentityResourceDisplayName = TestValues.NewDisplayName();

    private static readonly string ApiScopeDisplayName = TestValues.NewDisplayName();

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
        var claims = new[] { new Claim(JwtClaimTypes.Subject, SignedInSubjectId) };
        var identity = new ClaimsIdentity(claims, AuthenticationType);
        httpContext.User = new ClaimsPrincipal(identity);

        var model = CreateModel(
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
        Assert.Equal(GrantsModel.GrantsPagePath, redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_WithGrants_ClientFound_PopulatesViewModelCorrectly()
    {
        // Arrange
        var grant = new Grant
        {
            ClientId = GrantedClientId,
            Scopes = [IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile],
            CreationTime = TestValues.NewUtcDateTime(),
        };
        var client = new Client { ClientId = GrantedClientId, ClientName = ExistingClientName };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(GrantedClientId, It.IsAny<CancellationToken>())).ReturnsAsync(client);

        var mockResources = new Mock<IResourceStore>(MockBehavior.Strict);
        mockResources
            .Setup(x => x.FindIdentityResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<IdentityResource>)[new IdentityResource { Name = IdentityServerConstants.StandardScopes.OpenId, DisplayName = IdentityResourceDisplayName }]);
        mockResources
            .Setup(x => x.FindApiResourcesByScopeNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ApiResource>)[]);
        mockResources
            .Setup(x => x.FindApiScopesByNameAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<ApiScope>)[new ApiScope { Name = IdentityServerConstants.StandardScopes.Profile, DisplayName = ApiScopeDisplayName }]);

        var mockEvents = new Mock<IEventService>(MockBehavior.Strict);
        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object);

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
            CreationTime = TestValues.NewUtcDateTime(),
        };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(UnknownClientId, It.IsAny<CancellationToken>()))
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
        var grant1 = new Grant
        {
            ClientId = GrantedClientId,
            Scopes = [IdentityServerConstants.StandardScopes.OpenId],
            CreationTime = TestValues.NewUtcDateTime(),
        };
        var grant2 = new Grant
        {
            ClientId = UnknownClientId,
            Scopes = [IdentityServerConstants.StandardScopes.Profile],
            CreationTime = TestValues.NewUtcDateTime(),
        };
        var client1 = new Client { ClientId = GrantedClientId, ClientName = TestValues.NewClientName() };

        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction.Setup(x => x.GetAllUserGrantsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([grant1, grant2]);

        var mockClients = new Mock<IClientStore>(MockBehavior.Strict);
        mockClients.Setup(x => x.FindClientByIdAsync(GrantedClientId, It.IsAny<CancellationToken>())).ReturnsAsync(client1);
        mockClients.Setup(x => x.FindClientByIdAsync(UnknownClientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client?)null);

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

        var model = CreateModel(mockInteraction.Object, mockClients.Object, mockResources.Object, mockEvents.Object, httpContext);
        model.ClientId = RevokedClientId;

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(RevokedClientId, revokedClientId);
    }

    private static GrantsModel CreateModel(
        IIdentityServerInteractionService interaction,
        IClientStore clients,
        IResourceStore resources,
        IEventService events,
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