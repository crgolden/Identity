namespace Identity.Tests.Unit;

using Azure.Messaging.ServiceBus;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Identity.Avatar;
using Identity.CAPTCHA;
using Identity.Pages;
using Identity.Pages.Account;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Azure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ConstructorNullGuardTests : IDisposable
{
    private readonly TelemetryHarness _harness = new();

    [Fact]
    public void Constructor_NullBackchannelInteraction_Ciba_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<Ciba>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_RegisterConfirmation_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<RegisterConfirmation>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ChangePassword_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<ChangePassword>([null, signInManager]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_ChangePassword_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();

        // Act
        var result = ConstructorGuard.Construct<ChangePassword>([userManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_Logout_ThrowsArgumentNullException()
    {
        // Arrange
        var interactionService = Mock.Of<IIdentityServerInteractionService>();

        // Act
        var result = ConstructorGuard.Construct<Logout>([null, interactionService]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullInteractionService_Logout_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<Logout>([signInManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_LoginWithRecoveryCode_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<LoginWithRecoveryCode>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_Login_ThrowsArgumentNullException()
    {
        // Arrange
        var captchaService = Mock.Of<ICAPTCHAService>();

        // Act
        var result = ConstructorGuard.Construct<Login>([null, captchaService, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullCaptchaService_Login_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<Login>([signInManager, null, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_TwoFactorAuthentication_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<TwoFactorAuthentication>([null, signInManager]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_TwoFactorAuthentication_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();

        // Act
        var result = ConstructorGuard.Construct<TwoFactorAuthentication>([userManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ConfirmEmailChange_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<ConfirmEmailChange>([null, signInManager]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_ConfirmEmailChange_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();

        // Act
        var result = ConstructorGuard.Construct<ConfirmEmailChange>([userManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ConfirmEmail_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<ConfirmEmail>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_SetPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<SetPassword>([null, signInManager]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_SetPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();

        // Act
        var result = ConstructorGuard.Construct<SetPassword>([userManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ResetAuthenticator_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<ResetAuthenticator>([null, signInManager]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_ResetAuthenticator_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();

        // Act
        var result = ConstructorGuard.Construct<ResetAuthenticator>([userManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ForgotPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceBusClientFactory = Mock.Of<IAzureClientFactory<ServiceBusClient>>();
        var accountEmailSettings = TestValues.NewAccountEmailSettings();

        // Act
        var result = ConstructorGuard.Construct<ForgotPassword>([null, serviceBusClientFactory, accountEmailSettings]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullServiceBusClientFactory_ForgotPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();
        var accountEmailSettings = TestValues.NewAccountEmailSettings();

        // Act
        var result = ConstructorGuard.Construct<ForgotPassword>([userManager, null, accountEmailSettings]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullAccountEmailSettings_ForgotPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();
        var serviceBusClientFactory = Mock.Of<IAzureClientFactory<ServiceBusClient>>();

        // Act
        var result = ConstructorGuard.Construct<ForgotPassword>([userManager, serviceBusClientFactory, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_ExternalLogins_ThrowsArgumentNullException()
    {
        // Arrange
        var signInManager = NewSignInManager();
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        // Act
        var result = ConstructorGuard.Construct<ExternalLogins>([null, signInManager, userStore]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullSignInManager_ExternalLogins_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        // Act
        var result = ConstructorGuard.Construct<ExternalLogins>([userManager, null, userStore]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserStore_ExternalLogins_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();
        var signInManager = NewSignInManager();

        // Act
        var result = ConstructorGuard.Construct<ExternalLogins>([userManager, signInManager, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_DownloadPersonalData_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<DownloadPersonalData>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullInteraction_Grants_ThrowsArgumentNullException()
    {
        // Arrange
        var clients = Mock.Of<IClientStore>();
        var resources = Mock.Of<IResourceStore>();
        var events = Mock.Of<IEventService>();

        // Act
        var result = ConstructorGuard.Construct<Grants>([null, clients, resources, events, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullClients_Grants_ThrowsArgumentNullException()
    {
        // Arrange
        var interaction = Mock.Of<IIdentityServerInteractionService>();
        var resources = Mock.Of<IResourceStore>();
        var events = Mock.Of<IEventService>();

        // Act
        var result = ConstructorGuard.Construct<Grants>([interaction, null, resources, events, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullResources_Grants_ThrowsArgumentNullException()
    {
        // Arrange
        var interaction = Mock.Of<IIdentityServerInteractionService>();
        var clients = Mock.Of<IClientStore>();
        var events = Mock.Of<IEventService>();

        // Act
        var result = ConstructorGuard.Construct<Grants>([interaction, clients, null, events, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullEvents_Grants_ThrowsArgumentNullException()
    {
        // Arrange
        var interaction = Mock.Of<IIdentityServerInteractionService>();
        var clients = Mock.Of<IClientStore>();
        var resources = Mock.Of<IResourceStore>();

        // Act
        var result = ConstructorGuard.Construct<Grants>([interaction, clients, resources, null, _harness.Telemetry]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullUserManager_GenerateRecoveryCodes_ThrowsArgumentNullException()
    {
        // Act
        var result = ConstructorGuard.Construct<GenerateRecoveryCodes>([null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    [Fact]
    public void Constructor_NullAvatarService_AvatarProfileService_ThrowsArgumentNullException()
    {
        // Arrange
        var userManager = NewUserManager();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();

        // Act
        var result = ConstructorGuard.Construct<AvatarProfileService>([userManager, claimsFactory, null]);

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(result.Thrown);
        Assert.Equal(result.ExpectedParamName, argumentNull.ParamName);
    }

    public void Dispose() => _harness.Dispose();

    private static UserManager<IdentityUser<Guid>> NewUserManager() => MockHelpers.MockUserManager().Object;

    private static SignInManager<IdentityUser<Guid>> NewSignInManager() =>
        MockHelpers.MockSignInManager(NewUserManager()).Object;
}
