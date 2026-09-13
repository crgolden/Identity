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
public class DeviceIndexModelTests
{
    private static readonly string ExistingClientId = TestValues.NewClientIdentifier();
    private static readonly string ExistingClientName = TestValues.NewClientName();
    private static readonly string KnownUserCode = TestValues.NewVerificationCode();
    private static readonly string UnknownUserCode = TestValues.NewVerificationCode();
    private static readonly string ApiScopeName = TestValues.NewApiScopeName();
    private static readonly string ApiScopeDisplayName = TestValues.NewDisplayName();
    private static readonly string ApiResourceName = TestValues.NewApiResourceName();
    private static readonly string ApiResourceDisplayName = TestValues.NewDisplayName();
    private static readonly string SignedInSubjectId = TestValues.NewSubjectId();
    private static readonly string AuthenticationType = TestValues.NewSchemeName();

    [Fact]
    public void Constructor_ValidDependencies_CreatesPageModel()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object;
        var events = new Mock<IEventService>(MockBehavior.Strict).Object;

        // Act
        var model = new DeviceModel(interaction, events);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_NullUserCode_ReturnsPage()
    {
        // Arrange
        var model = CreateModel(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);

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
        var model = CreateModel(interaction.Object);

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
        var model = CreateModel(interaction.Object);

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
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync(KnownUserCode);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingClientId, model.View.ClientName);
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, ApiScopeName, StringComparison.Ordinal));
        Assert.Contains(model.View.ApiScopes, s => string.Equals(s.Value, IdentityServerConstants.StandardScopes.OfflineAccess, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnPostAsync_NullAuthorizationContext_RedirectsToError()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);
        model.Input = new DeviceModel.InputModel { UserCode = KnownUserCode };

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
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new DeviceModel.InputModel { Button = ConsentModel.DenyButtonValue, UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(DeviceModel.DeviceSuccessPagePath, Assert.IsType<RedirectToPageResult>(result).PageName);
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
        var model = CreateModel(interaction.Object, events.Object);
        model.Input = new DeviceModel.InputModel { Button = ConsentModel.GrantButtonValue, ScopesConsented = [IdentityServerConstants.StandardScopes.OpenId], UserCode = KnownUserCode };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal(DeviceModel.DeviceSuccessPagePath, Assert.IsType<RedirectToPageResult>(result).PageName);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.HandleRequestAsync(KnownUserCode, It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_NoScopes_AddsModelErrorAndReRenders()
    {
        // Arrange
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequest());
        var model = CreateModel(interaction.Object);
        model.Input = new DeviceModel.InputModel { Button = ConsentModel.GrantButtonValue, ScopesConsented = [], UserCode = KnownUserCode };

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
        var model = CreateModel(interaction.Object);
        model.Input = new DeviceModel.InputModel { Button = TestValues.NewButtonValue(), UserCode = KnownUserCode };

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
        var model = CreateModel(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);
        model.Input = new DeviceModel.InputModel { UserCode = null };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

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

    private static DeviceModel CreateModel(
        IDeviceFlowInteractionService interaction,
        IEventService? events = null)
    {
        var model = new DeviceModel(interaction, events ?? Mock.Of<IEventService>());
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