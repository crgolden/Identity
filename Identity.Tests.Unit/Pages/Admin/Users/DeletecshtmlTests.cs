namespace Identity.Tests.Unit.Pages.Admin.Users;

using Identity.Pages.Admin.Users;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);

        var model = new DeleteModel(um.Object);
        var result = await model.OnGetAsync("1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("alice", model.AppUser.UserName);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new DeleteModel(um.Object).OnGetAsync("99"));
    }

    [Fact]
    public async Task OnPostAsync_DeletesAndRedirects_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await new DeleteModel(um.Object).OnPostAsync("1");

        um.Verify(m => m.DeleteAsync(user), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new DeleteModel(um.Object).OnPostAsync("99"));
    }
}
