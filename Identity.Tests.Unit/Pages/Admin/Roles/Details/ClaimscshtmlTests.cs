namespace Identity.Tests.Unit.Pages.Admin.Roles.Details;

using System.Security.Claims;
using Identity.Pages.Admin.Roles.Details;
using Infrastructure;
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
        var role = new IdentityRole<Guid>("Admin");
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([new Claim("permission", "read")]);

        var model = new ClaimsModel(rm.Object);
        var result = await model.OnGetAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(rm.Object).OnGetAsync("missing"));
    }
}
