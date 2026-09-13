namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Buffers.Text;
using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class RenamePasskeyModelTests
{
    private static readonly byte[] CredentialIdBytes = TestValues.NewCredentialIdBytes();

    private static readonly string ValidCredentialId = Base64Url.EncodeToString(CredentialIdBytes);

    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    private static readonly string PasskeylessUserId = TestValues.NewUserId().ToString();

    private static readonly string ExistingPasskeyName = TestValues.NewDisplayName();

    [Fact]
    public void Constructor_ValidDependencies_InitializesInput()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var dbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>()).Object;

        // Act
        var model = new RenamePasskeyModel(userManager.Object, dbContext);

        // Assert
        Assert.NotNull(model.Input);
        Assert.Null(model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_InvalidFormatId_RedirectsToPasskeys()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());

        // Act
        var result = await model.OnGetAsync(TestValues.NewPunctuatedPageName());

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingPasskeys, redirect.PageName);
        Assert.Equal(RenamePasskeyModel.InvalidCredentialIdFormatMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_PasskeyNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync((UserPasskeyInfo?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(PasskeylessUserId);

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(PasskeylessUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_PasskeyFound_PopulatesInputAndReturnsPage()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync(BuildPasskey(ExistingPasskeyName));

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ValidCredentialId, model.Input.CredentialId);
        Assert.Equal(ExistingPasskeyName, model.Input.Name);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_InvalidFormatCredentialId_RedirectsToPasskeys()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = TestValues.NewPunctuatedPageName() };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingPasskeys, redirect.PageName);
        Assert.Equal(RenamePasskeyModel.InvalidCredentialIdFormatMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_PasskeyNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync((UserPasskeyInfo?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(PasskeylessUserId);
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = ValidCredentialId, Name = TestValues.NewApiResourceName() };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(PasskeylessUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_AddOrUpdateFails_Throws()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync(BuildPasskey(ExistingPasskeyName));
        userManager.Setup(m => m.AddOrUpdatePasskeyAsync(user, It.IsAny<UserPasskeyInfo>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = TestValues.NewFailureReason() }));
        userManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(TestValues.NewUserId().ToString());
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = ValidCredentialId, Name = TestValues.NewApiResourceName() };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static UserPasskeyInfo BuildPasskey(string name) =>
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
            Name = name,
        };

    private static (Mock<UserManager<IdentityUser<Guid>>> UserManager, RenamePasskeyModel Model) CreateModel()
    {
        var userManager = MockHelpers.MockUserManager();
        var dbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>()).Object;
        var model = new RenamePasskeyModel(userManager.Object, dbContext)
        {
            PageContext = MockHelpers.PageContext(),
        };
        return (userManager, model);
    }
}