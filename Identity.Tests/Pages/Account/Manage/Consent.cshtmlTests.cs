namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConsentIndexModelTests
{
    [Fact]
    public void Constructor_ValidDependencies_CreatesPageModel()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict).Object;
        var events = new Mock<IEventService>(MockBehavior.Strict).Object;

        // Act
        var model = new ConsentModel(interaction, events);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullReturnUrl_RedirectsToError()
    {
        // Arrange — blank returnUrl makes SetViewModelAsync return false
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidReturnUrl_InteractionReturnsNull_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("https://example.com");

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidReturnUrl_BuildsViewModelAndReturnsPage()
    {
        // Arrange — a constructed AuthorizationRequest drives the happy SetViewModelAsync + CreateConsentViewModel path
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequestWithIdentityScope());
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("https://example.com");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("https://example.com", model.Input.ReturnUrl);
        Assert.Equal("Client One", model.View.ClientName);
        Assert.NotEmpty(model.View.IdentityScopes);
    }

    [Fact]
    public async Task OnGetAsync_RichRequest_BuildsApiScopesWithResourcesAndOfflineAccess()
    {
        // Arrange — populated scopes/resources exercise the ClientName fallback, the api-scope loop,
        // FindApiScope-found, the resource indicator path, and the offline-access scope
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRichRequest());
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("https://example.com");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("client1", model.View.ClientName); // ClientName null → falls back to ClientId
        Assert.Contains(model.View.ApiScopes, s => s.Value == "api.read");
        Assert.Contains(model.View.ApiScopes, s => s.Resources.Any(r => r.DisplayName == "API One"));
        Assert.Contains(model.View.ApiScopes, s => s.Value == "offline_access");
    }

    [Fact]
    public async Task OnGetAsync_RequestWithUnknownScope_SkipsScopeWithoutApiScope()
    {
        // Arrange — a parsed scope FindApiScope cannot resolve is skipped; no offline scope when OfflineAccess is false
        var parsed = new[] { new ParsedScopeValue("unknown.scope") };
        var request = new AuthorizationRequest
        {
            Client = new Client { ClientId = "client1", ClientName = "Client One" },
            ValidatedResources = new ResourceValidationResult(new Resources(), parsed),
        };
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("https://example.com");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.View.ApiScopes);
    }

    [Fact]
    public async Task OnPostAsync_NullAuthorizationContext_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);
        model.Input = new ConsentModel.InputModel { ReturnUrl = "https://example.com" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostAsync_ButtonNo_RaisesDeniedEventAndRedirects()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        interaction.Setup(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), "user-123")).Returns(Task.CompletedTask);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel { Button = "no", ReturnUrl = "https://example.com" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("https://example.com", Assert.IsType<RedirectResult>(result).Url);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentDeniedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), "user-123"), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_WithScopes_RaisesGrantedEventAndRedirects()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        interaction.Setup(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), "user-123")).Returns(Task.CompletedTask);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel
        {
            Button = "yes",
            ScopesConsented = ["openid"],
            ReturnUrl = "https://example.com",
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("https://example.com", Assert.IsType<RedirectResult>(result).Url);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), "user-123"), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_NoScopesConsented_AddsModelErrorAndReRenders()
    {
        // Arrange — the genuine "yes + no scopes" path (defect D-2 fix): MustChooseOne error, then re-render
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var model = CreateModel(interaction.Object);
        model.Input = new ConsentModel.InputModel
        {
            Button = "yes",
            ScopesConsented = [],
            ReturnUrl = "https://example.com",
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_InvalidButton_SetViewModelFails_RedirectsToError()
    {
        // Arrange — unknown button → InvalidSelection error; the re-render's SetViewModelAsync then fails
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction
            .SetupSequence(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .ReturnsAsync((AuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);
        model.Input = new ConsentModel.InputModel { Button = "maybe", ReturnUrl = "https://example.com" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_GrantedConsentWithNullReturnUrl_Throws()
    {
        // Arrange — deny grants a consent response, but the null ReturnUrl trips ThrowIfNull before the redirect
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel { Button = "no", ReturnUrl = null };

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => model.OnPostAsync());
    }

    private static AuthorizationRequest BuildRequest() => new()
    {
        Client = new Client { ClientId = "client1", ClientName = "Client One" },
        ValidatedResources = new ResourceValidationResult(),
    };

    private static AuthorizationRequest BuildRequestWithIdentityScope()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        return new AuthorizationRequest
        {
            Client = new Client { ClientId = "client1", ClientName = "Client One" },
            ValidatedResources = new ResourceValidationResult(resources),
        };
    }

    private static AuthorizationRequest BuildRichRequest()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        resources.ApiScopes.Add(new ApiScope("api.read", "API Read"));
        resources.ApiResources.Add(new ApiResource("api1", "API One") { Scopes = { "api.read" } });
        resources.OfflineAccess = true;
        var parsed = new[] { new ParsedScopeValue("api.read", "api.read", "tenant1") };
        var request = new AuthorizationRequest
        {
            Client = new Client { ClientId = "client1" }, // ClientName null → fallback to ClientId
            ValidatedResources = new ResourceValidationResult(resources, parsed),
        };
        request.Parameters.Add(OidcConstants.AuthorizeRequest.Resource, "api1");
        return request;
    }

    private static ConsentModel CreateModel(
        IIdentityServerInteractionService interaction,
        IEventService? events = null)
    {
        var model = new ConsentModel(interaction, events ?? Mock.Of<IEventService>());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-123")], "test"));
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = new DefaultHttpContext { User = principal },
            RouteData = new RouteData(),
        };
        return model;
    }
}
