namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Buffers.Text;
using System.Globalization;
using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class PasskeysModelTests
{
    private static readonly byte[] CredentialIdBytes = TestValues.NewCredentialIdBytes();

    private static readonly string ValidCredentialId = Base64Url.EncodeToString(CredentialIdBytes);

    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    private static readonly string BrowserError = TestValues.NewFailureReason();

    private static readonly string AttestationFailure = TestValues.NewFailureReason();

    [Fact]
    public void Constructor_ValidManagers_InitializesProperties()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);

        // Act
        var model = new PasskeysModel(userManager.Object, signInManager.Object);

        // Assert
        Assert.NotNull(model.CurrentPasskeys);
        Assert.Empty(model.CurrentPasskeys);
        Assert.NotNull(model.Input);
        Assert.Null(model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_UserFound_LoadsPasskeysAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        var passkeys = new List<UserPasskeyInfo> { BuildPasskey() };
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync(passkeys);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Same(passkeys, model.CurrentPasskeys);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_BlankCredentialId_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = null };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PasskeysModel.PasskeyNotFoundMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_InvalidFormatCredentialId_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = TestValues.NewPunctuatedPageName() };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(RenamePasskeyModel.InvalidCredentialIdFormatMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionRename_RedirectsToRenamePasskey()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = PasskeysModel.RenameAction };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingRenamePasskey, redirect.PageName);
        Assert.Equal(ValidCredentialId, redirect.RouteValues?[RenamePasskeyModel.IdRouteValueName]);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionDelete_Success_RemovesAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Success);
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = PasskeysModel.DeleteAction };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PasskeysModel.PasskeyRemovedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionDelete_RemoveFails_Throws()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = TestValues.NewFailureReason() }));
        userManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(TestValues.NewUserId().ToString());
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = PasskeysModel.DeleteAction };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostUpdatePasskeyAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_UnknownAction_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = TestValues.NewPasskeyAction() };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PasskeysModel.UnknownActionMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_BrowserReportedError_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { Error = BrowserError } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, PasskeysModel.BrowserErrorMessageFormat, BrowserError),
            model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_NoCredentialJson_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { Error = null, CredentialJson = null } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PasskeysModel.BrowserProvidedNoPasskeyMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_AttestationFails_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        signInManager.Setup(s => s.PerformPasskeyAttestationAsync(It.IsAny<string>()))
            .ReturnsAsync(PasskeyAttestationResult.Fail(new PasskeyException(AttestationFailure)));
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = TestValues.NewPasskeyCredentialJson() } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, PasskeysModel.AttestationFailedMessageFormat, AttestationFailure),
            model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_AddOrUpdateFails_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        signInManager.Setup(s => s.PerformPasskeyAttestationAsync(It.IsAny<string>())).ReturnsAsync(BuildSuccessfulAttestation());
        userManager.Setup(m => m.AddOrUpdatePasskeyAsync(user, It.IsAny<UserPasskeyInfo>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = TestValues.NewFailureReason() }));
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = TestValues.NewPasskeyCredentialJson() } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PasskeysModel.PasskeyNotAddedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_Success_RedirectsToRenamePasskey()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        signInManager.Setup(s => s.PerformPasskeyAttestationAsync(It.IsAny<string>())).ReturnsAsync(BuildSuccessfulAttestation());
        userManager.Setup(m => m.AddOrUpdatePasskeyAsync(user, It.IsAny<UserPasskeyInfo>())).ReturnsAsync(IdentityResult.Success);
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = TestValues.NewPasskeyCredentialJson() } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingRenamePasskey, redirect.PageName);
        Assert.Equal(ValidCredentialId, redirect.RouteValues?[RenamePasskeyModel.IdRouteValueName]);
        Assert.Equal(PasskeysModel.PasskeyAddedMessage, model.StatusMessage);
    }

    private static UserPasskeyInfo BuildPasskey() =>
        new(
            credentialId: CredentialIdBytes,
            publicKey: TestValues.NewPublicKeyBytes(),
            createdAt: DateTimeOffset.UnixEpoch,
            signCount: 0,
            transports: null,
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: TestValues.NewAttestationObjectBytes(),
            clientDataJson: TestValues.NewClientDataJsonBytes())
        {
            Name = TestValues.NewApiResourceName(),
        };

    private static PasskeyAttestationResult BuildSuccessfulAttestation()
    {
        var entity = new PasskeyUserEntity
        {
            Id = TestValues.NewUserId().ToString(),
            Name = TestValues.NewEmailAddress(),
            DisplayName = TestValues.NewGivenName()
        };
        return PasskeyAttestationResult.Success(BuildPasskey(), entity);
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> UserManager, Mock<SignInManager<IdentityUser<Guid>>> SignInManager, PasskeysModel Model) CreateModel()
    {
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        var model = new PasskeysModel(userManager.Object, signInManager.Object)
        {
            PageContext = MockHelpers.PageContext(),
        };
        return (userManager, signInManager, model);
    }
}