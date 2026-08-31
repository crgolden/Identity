namespace Identity.Tests.Unit.Pages.Admin.Users;

using Identity.Pages.Admin.Users;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly string ExistingUserName = TestValues.NewUserName();

    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = ExistingUserName };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);

        var model = new DeleteModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingUserName, model.AppUser.UserName);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new DeleteModel(um.Object).OnGetAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAsync_DeletesAndRedirects_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = ExistingUserName };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await new DeleteModel(um.Object).OnPostAsync(ExistingUserId);

        um.Verify(m => m.DeleteAsync(user), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new DeleteModel(um.Object).OnPostAsync(MissingUserId));
    }
}