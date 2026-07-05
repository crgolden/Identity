namespace Identity.Tests.Unit.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Roles.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var role = new IdentityRole<Guid>("Admin") { Name = "Admin" };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([new Claim("permission", "read")]);

        var model = new ClaimsModel(rm.Object);
        var result = await model.OnGetAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Admin", model.RoleName);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(rm.Object).OnGetAsync("missing"));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        var role = new IdentityRole<Guid>("Admin");
        var existing = new Claim("old", "value");
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([existing]);
        rm.Setup(m => m.RemoveClaimAsync(role, existing)).ReturnsAsync(IdentityResult.Success);
        rm.Setup(m => m.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var model = new ClaimsModel(rm.Object) { Claims = [new Claim("new", "val")] };
        var result = await model.OnPostAsync(role.Id.ToString());

        rm.Verify(m => m.RemoveClaimAsync(role, existing), Times.Once);
        rm.Verify(m => m.AddClaimAsync(role, It.IsAny<Claim>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Roles/Details/Claims", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(rm.Object).OnPostAsync("missing"));
    }
}
