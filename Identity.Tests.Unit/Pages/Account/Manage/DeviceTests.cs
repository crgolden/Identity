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
public sealed class DeviceTests : IDisposable
{
    private static readonly string ExistingClientId = Generated.NewClientIdentifier();
    private static readonly string ExistingClientName = Generated.NewClientName();
    private static readonly string KnownUserCode = Generated.NewVerificationCode();
    private static readonly string UnknownUserCode = Generated.NewVerificationCode();
    private static readonly string ApiScopeName = Generated.NewApiScopeName();
    private static readonly string ApiScopeDisplayName = Generated.NewDisplayName();
    private static readonly string ApiResourceName = Generated.NewApiResourceName();
    private static readonly string ApiResourceDisplayName = Generated.NewDisplayName();
    private static readonly string SignedInSubjectId = Generated.NewSubjectId();
    private static readonly string AuthenticationType = Generated.NewSchemeName();
    private static readonly IOptions<ConsentOptions> OfflineAccessEnabled =
        Options.Create(new ConsentOptions(true, Generated.NewDisplayName(), Generated.NewDescription()));

    private readonly TelemetryHarness _harness = new();

    [Fact]
    public void Constructor_ValidDependencies_CreatesPageModel()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object;
        var events = new Mock<IEventService>(MockBehavior.Strict).Object;

        // Act
        var model = new Device(interaction, events, OfflineAccessEnabled, _harness.Telemetry);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullUserCode_ReturnsPage()
    {
        // Arrange
        var model = Create(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_InvalidUserCode_ReturnsPageWithModelError()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = Create(interaction.Object);

        // Act
        var result = await model.OnGetAsync(UnknownUserCode);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnGetAsync_ValidUserCode_SetsInputAndReturnsPage()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRequestWithIdentityScope());
        var model = Create(interaction.Object);

        // Act
        var result = await model.OnGetAsync(KnownUserCode);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(KnownUserCode, model.Input.UserCode);
        Assert.Equal(ExistingClientName, model.View.ClientName);
        Assert.NotEmpty(model.View.IdentityScopes);
    }

    [Fact]
    public async Task OnGetAsync_RichRequest_BuildsApiScopesWithResourcesAndOfflineAccess()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRichRequest());
        var model = Create(interaction.Object);

        // Act
        var result = await model.OnGetAsync(KnownUserCode);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingClientId, model.View.ClientName);
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, ApiScopeName, StringComparison.Ordinal));
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, IdentityServerConstants.StandardScopes.OfflineAccess, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnGetAsync_RequestWithUnknownScope_SkipsScopeWithoutApiScope()
    {
        // Arrange
        var parsed = new[] { new ParsedScopeValue(Generated.NewApiScopeName()) };
        var request = new DeviceFlowAuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
            ValidatedResources = new ResourceValidationResult(new Resources(), parsed),
        };
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        var model = Create(interaction.Object);

        // Act
        var result = await model.OnGetAsync(KnownUserCode);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.View.ApiScopes);
    }

    [Fact]
    public async Task OnGetAsync_RichRequest_ChecksEveryOptionalScopeOnFirstDisplay()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRichRequest());
        var model = Create(interaction.Object);

        // Act
        await model.OnGetAsync(KnownUserCode);

        // Assert
        Assert.All(model.View.ApiScopes, s => Assert.True(s.Checked));
    }

    [Fact]
    public async Task OnPostAsync_ReRender_ChecksOnlyTheConsentedScopes()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRichRequest());
        var model = Create(interaction.Object);
        model.Input = new Device.InputModel { Button = Generated.NewButtonValue(), ScopesConsented = [ApiScopeName], UserCode = KnownUserCode };

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.True(Assert.Single(model.View.ApiScopes, s => string.Equals(s.Value, ApiScopeName, StringComparison.Ordinal)).Checked);
        Assert.False(Assert.Single(model.View.ApiScopes, s => string.Equals(s.Value, IdentityServerConstants.StandardScopes.OfflineAccess, StringComparison.Ordinal)).Checked);
    }

    [Fact]
    public async Task OnPostAsync_NullAuthorizationContext_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = Create(interaction.Object);
        model.Input = new Device.InputModel { UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostAsync_ButtonNo_RaisesDeniedEventAndRedirectsToSuccess()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequest());
        interaction
            .Setup(x => x.HandleRequestAsync(It.IsAny<string>(), It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceFlowInteractionResult());
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = Create(interaction.Object, events.Object);
        model.Input = new Device.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.DenyButtonValue, UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(Device.DeviceSuccessPagePath, Assert.IsType<RedirectToPageResult>(result).PageName);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentDeniedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.HandleRequestAsync(KnownUserCode, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_WithScopes_RaisesGrantedEventAndRedirectsToSuccess()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequest());
        interaction
            .Setup(x => x.HandleRequestAsync(It.IsAny<string>(), It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceFlowInteractionResult());
        var events = new Mock<IEventService>(MockBehavior.Strict);
        events.Setup(e => e.RaiseAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = Create(interaction.Object, events.Object);
        model.Input = new Device.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue, ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId], UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(Device.DeviceSuccessPagePath, Assert.IsType<RedirectToPageResult>(result).PageName);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.HandleRequestAsync(KnownUserCode, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_NoScopes_AddsModelErrorAndReRenders()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequest());
        var model = Create(interaction.Object);
        model.Input = new Device.InputModel { Button = global::Identity.Pages.Account.Manage.Consent.GrantButtonValue, ScopesConsented = [], UserCode = KnownUserCode };

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
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .SetupSequence(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRequest())
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = Create(interaction.Object);
        model.Input = new Device.InputModel { Button = Generated.NewButtonValue(), UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(PageRoutes.Error, Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_NullUserCode_Throws()
    {
        // Arrange
        var model = Create(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);
        model.Input = new Device.InputModel { UserCode = null };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    public void Dispose() => _harness.Dispose();

    private static DeviceFlowAuthorizationRequest BuildRequest() => new()
    {
        Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
        ValidatedResources = new ResourceValidationResult(),
    };

    private static DeviceFlowAuthorizationRequest BuildRequestWithIdentityScope()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        return new DeviceFlowAuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId, ClientName = ExistingClientName },
            ValidatedResources = new ResourceValidationResult(resources),
        };
    }

    private static DeviceFlowAuthorizationRequest BuildRichRequest()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        resources.ApiScopes.Add(new ApiScope(ApiScopeName, ApiScopeDisplayName));
        resources.ApiResources.Add(new ApiResource(ApiResourceName, ApiResourceDisplayName) { Scopes = { ApiScopeName } });
        resources.OfflineAccess = true;
        var parsed = new[] { new ParsedScopeValue(ApiScopeName) };
        return new DeviceFlowAuthorizationRequest
        {
            Client = new Client { ClientId = ExistingClientId },
            ValidatedResources = new ResourceValidationResult(resources, parsed),
        };
    }

    private Device Create(
        IDeviceFlowInteractionService interaction,
        IEventService? events = null)
    {
        var model = new Device(interaction, events ?? Mock.Of<IEventService>(), OfflineAccessEnabled, _harness.Telemetry);
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
