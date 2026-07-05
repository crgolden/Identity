namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Buffers.Text;
using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class PasskeysModelTests
{
    private const string ValidCredentialId = "AQID"; // Base64Url of [1, 2, 3]

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
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
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
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
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
        Assert.Equal("Could not find the passkey.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_InvalidFormatCredentialId_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = "@@@" };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The specified passkey ID had an invalid format.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionRename_RedirectsToRenamePasskey()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = "rename" };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./RenamePasskey", redirect.PageName);
        Assert.Equal(ValidCredentialId, redirect.RouteValues?["id"]);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionDelete_Success_RemovesAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Success);
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = "delete" };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The passkey was removed.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_ActionDelete_RemoveFails_Throws()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "nope" }));
        userManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync("uid-1");
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = "delete" };

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostUpdatePasskeyAsync());
    }

    [Fact]
    public async Task OnPostUpdatePasskeyAsync_UnknownAction_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { CredentialId = ValidCredentialId, Action = "frobnicate" };

        // Act
        var result = await model.OnPostUpdatePasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Unknown action.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_BrowserReportedError_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { Error = "user cancelled" } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Could not add a passkey: user cancelled", model.StatusMessage);
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
        Assert.Equal("The browser did not provide a passkey.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAddPasskeyAsync_AttestationFails_SetsStatusAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        signInManager.Setup(s => s.PerformPasskeyAttestationAsync(It.IsAny<string>()))
            .ReturnsAsync(PasskeyAttestationResult.Fail(new PasskeyException("bad attestation")));
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = "{}" } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Could not add the passkey: bad attestation.", model.StatusMessage);
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
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "store failure" }));
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = "{}" } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The passkey could not be added to your account.", model.StatusMessage);
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
        model.Input = new PasskeysModel.InputModel { Passkey = new PasskeyInputModel { CredentialJson = "{}" } };

        // Act
        var result = await model.OnPostAddPasskeyAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./RenamePasskey", redirect.PageName);
        Assert.Equal(ValidCredentialId, redirect.RouteValues?["id"]);
        Assert.Equal("The passkey was added to your account. You can now use it to sign in. Give it an easy to remember name.", model.StatusMessage);
    }

    private static UserPasskeyInfo BuildPasskey() =>
        new(
            credentialId: [1, 2, 3],
            publicKey: [4, 5, 6],
            createdAt: DateTimeOffset.UnixEpoch,
            signCount: 0,
            transports: null,
            isUserVerified: false,
            isBackupEligible: false,
            isBackedUp: false,
            attestationObject: [7, 8, 9],
            clientDataJson: [10, 11, 12])
        {
            Name = "Test passkey",
        };

    private static PasskeyAttestationResult BuildSuccessfulAttestation()
    {
        var entity = new PasskeyUserEntity { Id = "user-1", Name = "user@example.com", DisplayName = "User" };
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
