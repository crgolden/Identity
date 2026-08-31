namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginscshtmlTests
{
    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetLoginsAsync(user)).ReturnsAsync([new UserLoginInfo("google", "key-1", "Google")]);

        var model = new LoginsModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        var onlyLogin = Assert.Single(model.Logins);
        Assert.Equal("google", onlyLogin.LoginProvider);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new LoginsModel(um.Object).OnGetAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostRemoveAsync_RemovesAndRedirects_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.RemoveLoginAsync(user, "google", "key-1")).ReturnsAsync(IdentityResult.Success);

        var result = await new LoginsModel(um.Object).OnPostRemoveAsync(ExistingUserId, "google", "key-1");

        um.Verify(m => m.RemoveLoginAsync(user, "google", "key-1"), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new LoginsModel(um.Object).OnPostRemoveAsync(MissingUserId, "google", "key-1"));
    }
}