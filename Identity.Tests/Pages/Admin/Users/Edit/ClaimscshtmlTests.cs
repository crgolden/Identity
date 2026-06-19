namespace Identity.Tests.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Users.Edit;
using Identity.Tests.Infrastructure;
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
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim("role", "Admin")]);

        var model = new ClaimsModel(um.Object);
        var result = await model.OnGetAsync("1");

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
        Assert.Equal("role", model.Claims[0].Type);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(um.Object).OnGetAsync("99"));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = "alice" };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim("old", "val")]);
        um.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);

        var model = new ClaimsModel(um.Object)
        {
            Claims = [new ClaimsModel.ClaimInputModel { Type = "role", Value = "Admin" }],
        };
        var result = await model.OnPostAsync("1");

        um.Verify(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        um.Verify(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Users/Details/Claims", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ClaimsModel(um.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync("99"));
    }
}
