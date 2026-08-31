namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RolescshtmlTests
{
    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var model = new RolesModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        var onlyRole = Assert.Single(model.Roles);
        Assert.Equal("Admin", onlyRole);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new RolesModel(um.Object).OnGetAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesRoles_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["OldRole"]);
        um.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        var model = new RolesModel(um.Object) { Roles = ["Admin"] };
        var result = await model.OnPostAsync(ExistingUserId);

        um.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        um.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Users/Details/Roles", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new RolesModel(um.Object) { Roles = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);

        var model = new RolesModel(um.Object) { Roles = [] };
        var result = await model.OnPostAddRowAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Roles);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new RolesModel(um.Object) { Roles = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);

        var model = new RolesModel(um.Object) { Roles = ["Admin"] };
        var result = await model.OnPostRemoveRowAsync(ExistingUserId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Roles);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new RolesModel(um.Object) { Roles = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingUserId, 0));
    }
}