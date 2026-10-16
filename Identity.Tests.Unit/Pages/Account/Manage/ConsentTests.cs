namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ConsentTests : IDisposable
{
    private static readonly string ExistingClientId = Generated.NewClientIdentifier();
    private static readonly string ExistingClientName = Generated.NewClientName();
    private static readonly string AuthorizeReturnUrl = Generated.NewCallbackAddress();
    private static readonly string ApiScopeName = Generated.NewApiScopeName();
    private static readonly string ApiScopeDisplayName = Generated.NewDisplayName();
    private static readonly string ApiResourceName = Generated.NewApiResourceName();
    private static readonly string ApiResourceDisplayName = Generated.NewDisplayName();
    private static readonly string ResourceIndicatorTenant = Generated.NewTenantName();
    private static readonly string SignedInSubjectId = Generated.NewSubjectId();
    private static readonly string AuthenticationType = Generated.NewSchemeName();
    private static readonly IOptions<ConsentOptions> OfflineAccessEnabled =
        Options.Create(new ConsentOptions(true, Generated.NewDisplayName(), Generated.NewDescription()));

    private static readonly IOptions<ConsentOptions> OfflineAccessDisabled =
        Options.Create(new ConsentOptions(false, Generated.NewDisplayName(), Generated.NewDescription()));

    private readonly TelemetryHarness _harness = new();

    [Fact]
    public void InputModel_New_HasNoScopesConsented()
    {
        // Act
        var input = new global::Identity.Pages.Account.Manage.Consent.InputModel();

        // Assert
        Assert.Empty(input.ScopesConsented);
    }

    [Fact]
    public void Constructor_ValidDependencies_CreatesPageModel()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict).Object;
        var events = new Mock<IEventService>(MockBehavior.Strict).Object;

        // Act
        var model = new global::Identity.Pages.Account.Manage.Consent(interaction, events, OfflineAccessEnabled, _harness.Telemetry);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullReturnUrl_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = Create(interaction.Object);

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
        var model = Create(interaction.Object);

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
        var model = Create(interaction.Object);

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
        var model = Create(interaction.Object);

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
    public async Task OnGetAsync_RichRequest_ChecksEveryOptionalScopeOnFirstDisplay()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRichRequest());
        var model = Create(interaction.Object);

        // Act
        await model.OnGetAsync(AuthorizeReturnUrl);

        // Assert
        Assert.All(model.View.ApiScopes, s => Assert.True(s.Checked));
    }

    [Fact]
    public async Task OnPostAsync_ReRender_ChecksOnlyTheConsentedScopes()
    {
        // Arrange
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRichRequest());
        var model = Create(interaction.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = Generated.NewButtonValue(),
            ScopesConsented = [ApiScopeName],
            ReturnUrl = AuthorizeReturnUrl,
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.True(Assert.Single(model.View.ApiScopes, s => string.Equals(s.Value, ApiScopeName, StringComparison.Ordinal)).Checked);
        Assert.False(Assert.Single(model.View.ApiScopes, s => string.Equals(s.Value, IdentityServerConstants.StandardScopes.OfflineAccess, StringComparison.Ordinal)).Checked);
    }

    [Fact]
    public async Task OnGetAsync_RequestWithUnknownScope_SkipsScopeWithoutApiScope()
    {
        // Arrange
        var parsed = new[] { new ParsedScopeValue(Generated.NewApiScopeName()) };
        var request = new AuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
            ValidatedResources = new ResourceValidationResult(new Resources(), parsed),
        };
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        var model = Create(interaction.Object);

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
        var model = Create(interaction.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel { ReturnUrl = AuthorizeReturnUrl };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.True(model.ModelState.IsValid);
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
        var model = Create(interaction.Object, events.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.DenyButtonValue, ReturnUrl = AuthorizeReturnUrl };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(AuthorizeReturnUrl, Assert.IsType<RedirectResult>(result).Url);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentDeniedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonNo_GrantsAnAccessDeniedResponse()
    {
        // Arrange
        var request = BuildRequest();
        var granted = new List<ConsentResponse>();
        var interaction = InteractionRecordingGrants(request, granted);
        var model = Create(interaction.Object, AcceptingEvents());
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.DenyButtonValue, ReturnUrl = AuthorizeReturnUrl };
        var denials = new List<long>();
        using var deniedListener = _harness.ListenTo(global::Identity.Telemetry.Metrics.ConsentDeniedCounterName, (value, _) => denials.Add(value));

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(InteractionError.AccessDenied, Assert.Single(granted).Error);
        Assert.Single(denials);
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, global::Identity.Pages.Account.Manage.Consent.DenyActivityName, StringComparison.Ordinal));
        Assert.Equal(ExistingClientId, span.GetTagItem(global::Identity.Telemetry.Metrics.ClientIdTagName));
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_OfflineAccessDisabled_DropsOfflineAccessFromTheGrant()
    {
        // Arrange
        var request = BuildRequest();
        var granted = new List<ConsentResponse>();
        var interaction = InteractionRecordingGrants(request, granted);
        var model = Create(interaction.Object, AcceptingEvents(), OfflineAccessDisabled);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue,
            ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.OfflineAccess],
            ReturnUrl = AuthorizeReturnUrl,
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(new[] { IdentityServerConstants.StandardScopes.OpenId }, Assert.Single(granted).ScopesValuesConsented);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_OfflineAccessEnabled_KeepsOfflineAccessInTheGrant()
    {
        // Arrange
        var request = BuildRequest();
        var granted = new List<ConsentResponse>();
        var interaction = InteractionRecordingGrants(request, granted);
        var model = Create(interaction.Object, AcceptingEvents());
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue,
            ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.OfflineAccess],
            ReturnUrl = AuthorizeReturnUrl,
        };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.Equal(new[] { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.OfflineAccess }, Assert.Single(granted).ScopesValuesConsented);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_GrantCarriesTheRememberChoiceAndDescription()
    {
        // Arrange
        var request = BuildRequest();
        var granted = new List<ConsentResponse>();
        var interaction = InteractionRecordingGrants(request, granted);
        var model = Create(interaction.Object, AcceptingEvents());
        var consentDescription = Generated.NewDescription();
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue,
            ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId],
            RememberConsent = true,
            Description = consentDescription,
            ReturnUrl = AuthorizeReturnUrl,
        };
        var grants = new List<long>();
        var denials = new List<long>();
        using var grantedListener = _harness.ListenTo(global::Identity.Telemetry.Metrics.ConsentGrantedCounterName, (value, _) => grants.Add(value));
        using var deniedListener = _harness.ListenTo(global::Identity.Telemetry.Metrics.ConsentDeniedCounterName, (value, _) => denials.Add(value));

        // Act
        await model.OnPostAsync();

        // Assert
        var grant = Assert.Single(granted);
        Assert.True(grant.RememberConsent);
        Assert.Equal(consentDescription, grant.Description);
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, global::Identity.Pages.Account.Manage.Consent.GrantActivityName, StringComparison.Ordinal));
        Assert.Equal(ExistingClientId, span.GetTagItem(global::Identity.Telemetry.Metrics.ClientIdTagName));
        Assert.Equal(Assert.IsType<IEnumerable<string>>(grant.ScopesValuesConsented, exactMatch: false).Count(), Assert.IsType<int>(span.GetTagItem(global::Identity.Telemetry.Metrics.ScopeCountTagName)));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(global::Identity.Telemetry.Metrics.RememberTagName)));
        Assert.Single(grants);
        Assert.Single(denials);
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
        var model = Create(interaction.Object, events.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue,
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
        var model = Create(interaction.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel
        {
            Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue,
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
        var model = Create(interaction.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel { Button = Generated.NewButtonValue(), ReturnUrl = AuthorizeReturnUrl };

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
        var model = Create(interaction.Object, events.Object);
        model.Input = new global::Identity.Pages.Account.Manage.Consent.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.DenyButtonValue, ReturnUrl = null };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    public void Dispose() => _harness.Dispose();

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

    private static Mock<IIdentityServerInteractionService> InteractionRecordingGrants(AuthorizationRequest request, List<ConsentResponse> granted)
    {
        var interaction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        interaction
            .Setup(x => x.GrantConsentAsync(request, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>(), SignedInSubjectId))
            .Callback<AuthorizationRequest, ConsentResponse, CancellationToken, string>((_, response, _, _) => granted.Add(response))
            .Returns(Task.CompletedTask);
        return interaction;
    }

    private static IEventService AcceptingEvents()
    {
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return events.Object;
    }

    private global::Identity.Pages.Account.Manage.Consent Create(
        IIdentityServerInteractionService interaction,
        IEventService? events = null,
        IOptions<ConsentOptions>? consentOptions = null)
    {
        var model = new global::Identity.Pages.Account.Manage.Consent(interaction, events ?? Mock.Of<IEventService>(), consentOptions ?? OfflineAccessEnabled, _harness.Telemetry);
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
