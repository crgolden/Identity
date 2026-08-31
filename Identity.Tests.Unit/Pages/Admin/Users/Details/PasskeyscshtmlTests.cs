namespace Identity.Tests.Unit.Pages.Admin.Users.Details;

using Identity.Pages.Admin.Users.Details;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PasskeyscshtmlTests
{
    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync([BuildPasskey()]);

        var model = new PasskeysModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Passkeys);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new PasskeysModel(um.Object).OnGetAsync(MissingUserId));
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
            Name = TestValues.NewApiResourceName(),
        };
}