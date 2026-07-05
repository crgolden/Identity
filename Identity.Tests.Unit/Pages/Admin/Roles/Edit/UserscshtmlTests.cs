namespace Identity.Tests.Unit.Pages.Admin.Roles.Edit;

using Identity.Pages.Admin.Roles.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class UserscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var role = new IdentityRole<Guid>("Admin") { Name = "Admin" };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync([MockHelpers.TestUser()]);

        var model = new UsersModel(rm.Object, um.Object);
        var result = await model.OnGetAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Users);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new UsersModel(rm.Object, MockHelpers.MockUserManager().Object).OnGetAsync("missing"));
    }
}
