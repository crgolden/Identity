namespace Identity.Tests.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RolescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var model = new RolesModel(um.Object);
        var result = await model.OnGetAsync("1");

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Roles);
        Assert.Equal("Admin", model.Roles[0]);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new RolesModel(um.Object).OnGetAsync("99"));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesRoles_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["OldRole"]);
        um.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        var model = new RolesModel(um.Object) { Roles = ["Admin"] };
        var result = await model.OnPostAsync("1");

        um.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        um.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Users/Details/Roles", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new RolesModel(um.Object) { Roles = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync("99"));
    }
}
