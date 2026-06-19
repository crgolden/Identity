namespace Identity.Tests.Pages.Admin.Roles.Details;

using Identity.Pages.Admin.Roles.Details;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var role = new IdentityRole<Guid>("Admin");
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);

        var model = new IndexModel(rm.Object);
        var result = await model.OnGetAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Admin", model.AppRole.Name);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new IndexModel(rm.Object).OnGetAsync("missing"));
    }
}
