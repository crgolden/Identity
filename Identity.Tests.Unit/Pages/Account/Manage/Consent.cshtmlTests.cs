namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConsentIndexModelTests
{
    private static readonly string ExistingClientId = TestValues.NewClientIdentifier();
    private static readonly string ExistingClientName = TestValues.NewClientName();
    private static readonly string AuthorizeReturnUrl = TestValues.NewCallbackUrl();
    private static readonly string ApiScopeName = TestValues.NewApiScopeName();
    private static readonly string ApiScopeDisplayName = TestValues.NewDisplayName();
    private static readonly string ApiResourceName = TestValues.NewApiResourceName();
    private static readonly string ApiResourceDisplayName = TestValues.NewDisplayName();
    private static readonly string ResourceIndicatorTenant = TestValues.NewTenantName();
    private static readonly string SignedInSubjectId = TestValues.NewSubjectId();
    private static readonly string AuthenticationType = TestValues.NewSchemeName();

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
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidReturnUrl_InteractionReturnsNull_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(AuthorizeReturnUrl);

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidReturnUrl_BuildsViewModelAndReturnsPage()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequestWithIdentityScope());
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(AuthorizeReturnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(AuthorizeReturnUrl, model.Input.ReturnUrl);
        Assert.Equal(ExistingClientName, model.View.ClientName);
        Assert.NotEmpty(model.View.IdentityScopes);
    }

    [Fact]
    public async Task OnGetAsync_RichRequest_BuildsApiScopesWithResourcesAndOfflineAccess()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRichRequest());
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(AuthorizeReturnUrl);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingClientId, model.View.ClientName);
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, ApiScopeName, StringComparison.Ordinal));
        Assert.Contains(model.View.ApiScopes, s => s.Resources.Any(r => string.Equals(r.DisplayName, ApiResourceDisplayName, StringComparison.Ordinal)));
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, IdentityServerConstants.StandardScopes.OfflineAccess, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnGetAsync_RequestWithUnknownScope_SkipsScopeWithoutApiScope()
    {
        // Arrange
        var parsed = new[] { new ParsedScopeValue(TestValues.NewApiScopeName()) };
        var request = new AuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
            ValidatedResources = new ResourceValidationResult(new Resources(), parsed),
        };
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(AuthorizeReturnUrl);

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
        model.Input = new ConsentModel.InputModel { ReturnUrl = AuthorizeReturnUrl };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostAsync_ButtonNo_RaisesDeniedEventAndRedirects()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        interaction.Setup(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId)).Returns(Task.CompletedTask);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel { Button = ConsentModel.DenyButtonValue, ReturnUrl = AuthorizeReturnUrl };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(AuthorizeReturnUrl, Assert.IsType<RedirectResult>(result).Url);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentDeniedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_WithScopes_RaisesGrantedEventAndRedirects()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        interaction.Setup(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId)).Returns(Task.CompletedTask);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel
        {
            Button = ConsentModel.GrantButtonValue,
            ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId],
            ReturnUrl = AuthorizeReturnUrl,
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(AuthorizeReturnUrl, Assert.IsType<RedirectResult>(result).Url);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_NoScopesConsented_AddsModelErrorAndReRenders()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var model = CreateModel(interaction.Object);
        model.Input = new ConsentModel.InputModel
        {
            Button = ConsentModel.GrantButtonValue,
            ScopesConsented = [],
            ReturnUrl = AuthorizeReturnUrl,
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
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction
            .SetupSequence(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request)
            .ReturnsAsync((AuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);
        model.Input = new ConsentModel.InputModel { Button = TestValues.NewButtonValue(), ReturnUrl = AuthorizeReturnUrl };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_GrantedConsentWithNullReturnUrl_Throws()
    {
        // Arrange
        var request = BuildRequest();
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new ConsentModel.InputModel { Button = ConsentModel.DenyButtonValue, ReturnUrl = null };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    private static AuthorizationRequest BuildRequest() => new()
    {
        Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
        ValidatedResources = new ResourceValidationResult(),
    };

    private static AuthorizationRequest BuildRequestWithIdentityScope()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        return new AuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
            ValidatedResources = new ResourceValidationResult(resources),
        };
    }

    private static AuthorizationRequest BuildRichRequest()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        resources.ApiScopes.Add(new ApiScope(ApiScopeName, ApiScopeDisplayName));
        resources.ApiResources.Add(new ApiResource(ApiResourceName, ApiResourceDisplayName) { Scopes = { ApiScopeName } });
        resources.OfflineAccess = true;
        var parsed = new[] { new ParsedScopeValue(ApiScopeName, ApiScopeName, ResourceIndicatorTenant) };
        var request = new AuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId },
            ValidatedResources = new ResourceValidationResult(resources, parsed),
        };
        request.Parameters.Add(OidcConstants.AuthorizeRequest.Resource, ApiResourceName);
        return request;
    }

    private static ConsentModel CreateModel(
        IIdentityServerInteractionService interaction,
        IEventService? events = null)
    {
        var model = new ConsentModel(interaction, events ?? Mock.Of<IEventService>());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtClaimTypes.Subject, SignedInSubjectId)], AuthenticationType));
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = new DefaultHttpContext { User = principal },
            RouteData = new RouteData(),
        };
        return model;
    }
}