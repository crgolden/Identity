namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class RenamePasskeyModelTests
{
    private const string ValidCredentialId = "AQID"; // Base64Url of [1, 2, 3]

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
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_InvalidFormatId_RedirectsToPasskeys()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());

        // Act
        var result = await model.OnGetAsync("@@@");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Passkeys", redirect.PageName);
        Assert.Equal("The specified passkey ID had an invalid format.", model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_PasskeyNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync((UserPasskeyInfo?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user-42");

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("user-42", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGetAsync_PasskeyFound_PopulatesInputAndReturnsPage()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync(BuildPasskey("Old name"));

        // Act
        var result = await model.OnGetAsync(ValidCredentialId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ValidCredentialId, model.Input.CredentialId);
        Assert.Equal("Old name", model.Input.Name);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_InvalidFormatCredentialId_RedirectsToPasskeys()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = "@@@" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Passkeys", redirect.PageName);
        Assert.Equal("The specified passkey ID had an invalid format.", model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_PasskeyNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(MockHelpers.TestUser());
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync((UserPasskeyInfo?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user-7");
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = ValidCredentialId, Name = "x" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("user-7", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_AddOrUpdateFails_Throws()
    {
        // Arrange
        var (userManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync(BuildPasskey("Old name"));
        userManager.Setup(m => m.AddOrUpdatePasskeyAsync(user, It.IsAny<UserPasskeyInfo>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));
        userManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync("uid-1");
        model.Input = new RenamePasskeyModel.InputModel { CredentialId = ValidCredentialId, Name = "New name" };

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync());
    }

    private static UserPasskeyInfo BuildPasskey(string name) =>
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
