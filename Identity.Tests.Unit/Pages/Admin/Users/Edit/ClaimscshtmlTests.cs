namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim("role", "Admin")]);

        var model = new ClaimsModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        var onlyClaim = Assert.Single(model.Claims);
        Assert.Equal("role", onlyClaim.Type);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(um.Object).OnGetAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim("old", "val")]);
        um.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);

        var model = new ClaimsModel(um.Object)
        {
            Claims = [new ClaimsModel.ClaimInputModel { Type = "role", Value = "Admin" }],
        };
        var result = await model.OnPostAsync(ExistingUserId);

        um.Verify(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        um.Verify(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Users/Details/Claims", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ClaimsModel(um.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);

        var model = new ClaimsModel(um.Object) { Claims = [] };
        var result = await model.OnPostAddRowAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ClaimsModel(um.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);

        var model = new ClaimsModel(um.Object) { Claims = [new ClaimsModel.ClaimInputModel { Type = "role", Value = "Admin" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingUserId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Claims);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ClaimsModel(um.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingUserId, 0));
    }
}