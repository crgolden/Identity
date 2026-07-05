namespace Identity.Tests.Unit.Pages.Admin.Roles.Edit;

using Identity.Pages.Admin.Roles.Edit;
using Identity.Tests.Unit.Infrastructure;
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

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenFound()
    {
        var role = new IdentityRole<Guid>("OldName");
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.UpdateAsync(role)).ReturnsAsync(IdentityResult.Success);

        var model = new IndexModel(rm.Object) { AppRole = new IdentityRole<Guid> { Name = "NewName" } };
        var result = await model.OnPostAsync(role.Id.ToString());

        Assert.Equal("NewName", role.Name);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Roles/Details/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        var model = new IndexModel(rm.Object) { AppRole = new IdentityRole<Guid> { Name = "X" } };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync("missing"));
    }
}
