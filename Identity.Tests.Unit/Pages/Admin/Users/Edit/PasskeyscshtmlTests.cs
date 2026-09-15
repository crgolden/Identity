namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using System.Buffers.Text;
using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PasskeyscshtmlTests
{
    private static readonly byte[] CredentialIdBytes = TestValues.NewCredentialIdBytes();

    private static readonly string ValidCredentialId = Base64Url.EncodeToString(CredentialIdBytes);

    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync([BuildPasskey()]);
        var model = new PasskeysModel(um.Object);

        // Act
        var result = await model.OnGetAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Passkeys);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new PasskeysModel(um.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_RemovesAndRedirects_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Success);
        var model = new PasskeysModel(um.Object);

        // Act
        var result = await model.OnPostRemoveAsync(ExistingUserId, ValidCredentialId);

        // Assert
        um.Verify(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>()), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new PasskeysModel(um.Object);

        // Act
        var result = await model.OnPostRemoveAsync(MissingUserId, ValidCredentialId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
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
}