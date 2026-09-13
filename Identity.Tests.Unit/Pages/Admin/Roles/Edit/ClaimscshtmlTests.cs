namespace Identity.Tests.Unit.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Roles.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    private static readonly string RoleName = TestValues.NewRoleName();

    private static readonly string ClaimType = TestValues.NewClaimType();

    private static readonly string ClaimValue = TestValues.NewClaimValue();

    private static readonly string RemovedClaimType = TestValues.NewClaimType();

    private static readonly string RemovedClaimValue = TestValues.NewClaimValue();

    private static readonly string PostedClaimType = TestValues.NewClaimType();

    private static readonly string PostedClaimValue = TestValues.NewClaimValue();

    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([new Claim(ClaimType, ClaimValue)]);

        var model = new ClaimsModel(rm.Object);
        var result = await model.OnGetAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Equal(RoleName, model.RoleName);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(rm.Object).OnGetAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        var role = new IdentityRole<Guid>(RoleName);
        var existing = new Claim(RemovedClaimType, RemovedClaimValue);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([existing]);
        rm.Setup(m => m.RemoveClaimAsync(role, existing)).ReturnsAsync(IdentityResult.Success);
        rm.Setup(m => m.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        var model = new ClaimsModel(rm.Object) { Claims = [new ClaimsModel.ClaimInputModel { Type = PostedClaimType, Value = PostedClaimValue }] };
        var result = await model.OnPostAsync(role.Id.ToString());

        rm.Verify(m => m.RemoveClaimAsync(role, existing), Times.Once);
        rm.Verify(m => m.AddClaimAsync(role, It.IsAny<Claim>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ClaimsModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(rm.Object).OnPostAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);

        var model = new ClaimsModel(rm.Object) { Claims = [] };
        var result = await model.OnPostAddRowAsync(role.Id.ToString());

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        var model = new ClaimsModel(rm.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingUserId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);

        var model = new ClaimsModel(rm.Object) { Claims = [new ClaimsModel.ClaimInputModel { Type = ClaimType, Value = ClaimValue }] };
        var result = await model.OnPostRemoveRowAsync(role.Id.ToString(), 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Claims);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        var model = new ClaimsModel(rm.Object) { Claims = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingUserId, 0));
    }
}