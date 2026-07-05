namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
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
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeviceIndexModelTests
{
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
        // Arrange — blank user code short-circuits to the entry form
        var model = CreateModel(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);

        // Act
        var result = await model.OnGetAsync(null);

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_InvalidUserCode_ReturnsPageWithModelError()
    {
        // Arrange — SetViewModelAsync returns false (no context) → model error + page
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("invalid-code");

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
        var result = await model.OnGetAsync("device-code");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("device-code", model.Input.UserCode);
        Assert.Equal("Client One", model.View.ClientName);
        Assert.NotEmpty(model.View.IdentityScopes);
    }

    [Fact]
    public async Task OnGetAsync_RichRequest_BuildsApiScopesWithResourcesAndOfflineAccess()
    {
        // Arrange — populated scopes/resources exercise the ClientName fallback, api-scope loop, and offline scope
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRichRequest());
        var model = CreateModel(interaction.Object);

        // Act
        var result = await model.OnGetAsync("device-code");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("client1", model.View.ClientName); // ClientName null → ClientId
        Assert.Contains(model.View.ApiScopes, s => s.Value == "api.read");
        Assert.Contains(model.View.ApiScopes, s => s.Value == "offline_access");
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
        model.Input = new DeviceModel.InputModel { UserCode = "device-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
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
        model.Input = new DeviceModel.InputModel { Button = "no", UserCode = "device-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Account/Manage/DeviceSuccess", Assert.IsType<RedirectToPageResult>(result).PageName);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentDeniedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.HandleRequestAsync("device-code", It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()), Times.Once);
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
        model.Input = new DeviceModel.InputModel { Button = "yes", ScopesConsented = ["openid"], UserCode = "device-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Account/Manage/DeviceSuccess", Assert.IsType<RedirectToPageResult>(result).PageName);
        events.Verify(e => e.RaiseAsync(It.IsAny<ConsentGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        interaction.Verify(x => x.HandleRequestAsync("device-code", It.IsAny<ConsentResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ButtonYes_NoScopes_AddsModelErrorAndReRenders()
    {
        // Arrange — "yes" with no scopes adds MustChooseOne, then re-renders via SetViewModelAsync
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(BuildRequest());
        var model = CreateModel(interaction.Object);
        model.Input = new DeviceModel.InputModel { Button = "yes", ScopesConsented = [], UserCode = "device-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_InvalidButton_SetViewModelFails_RedirectsToError()
    {
        // Arrange — unknown button adds InvalidSelection; the re-render's SetViewModelAsync then fails
        var interaction = new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict);
        interaction
            .SetupSequence(x => x.GetAuthorizationContextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildRequest())
            .ReturnsAsync((DeviceFlowAuthorizationRequest?)null);
        var model = CreateModel(interaction.Object);
        model.Input = new DeviceModel.InputModel { Button = "maybe", UserCode = "device-code" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.Equal("/Error", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_NullUserCode_Throws()
    {
        // Arrange — a null user code trips ThrowIfNull before any interaction call
        var model = CreateModel(new Mock<IDeviceFlowInteractionService>(MockBehavior.Strict).Object);
        model.Input = new DeviceModel.InputModel { UserCode = null };

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => model.OnPostAsync());
    }

    private static DeviceFlowAuthorizationRequest BuildRequest() => new()
    {
        Client = new Client { ClientId = "client1", ClientName = "Client One" },
        ValidatedResources = new ResourceValidationResult(),
    };

    private static DeviceFlowAuthorizationRequest BuildRequestWithIdentityScope()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        return new DeviceFlowAuthorizationRequest
        {
            Client = new Client { ClientId = "client1", ClientName = "Client One" },
            ValidatedResources = new ResourceValidationResult(resources),
        };
    }

    private static DeviceFlowAuthorizationRequest BuildRichRequest()
    {
        var resources = new Resources();
        resources.IdentityResources.Add(new IdentityResources.OpenId());
        resources.ApiScopes.Add(new ApiScope("api.read", "API Read"));
        resources.ApiResources.Add(new ApiResource("api1", "API One") { Scopes = { "api.read" } });
        resources.OfflineAccess = true;
        var parsed = new[] { new ParsedScopeValue("api.read") };
        return new DeviceFlowAuthorizationRequest
        {
            Client = new Client { ClientId = "client1" }, // ClientName null → fallback to ClientId
            ValidatedResources = new ResourceValidationResult(resources, parsed),
        };
    }

    private static DeviceModel CreateModel(
        IDeviceFlowInteractionService interaction,
        IEventService? events = null)
    {
        var model = new DeviceModel(interaction, events ?? Mock.Of<IEventService>());
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
