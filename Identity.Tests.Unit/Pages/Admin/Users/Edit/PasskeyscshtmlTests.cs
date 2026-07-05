namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PasskeyscshtmlTests
{
    private const string ValidCredentialId = "AQID";

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync([BuildPasskey()]);

        var model = new PasskeysModel(um.Object);
        var result = await model.OnGetAsync("1");

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Passkeys);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new PasskeysModel(um.Object).OnGetAsync("99"));
    }

    [Fact]
    public async Task OnPostRemoveAsync_RemovesAndRedirects_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>())).ReturnsAsync(IdentityResult.Success);

        var result = await new PasskeysModel(um.Object).OnPostRemoveAsync("1", ValidCredentialId);

        um.Verify(m => m.RemovePasskeyAsync(user, It.IsAny<byte[]>()), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new PasskeysModel(um.Object).OnPostRemoveAsync("99", ValidCredentialId));
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
}
