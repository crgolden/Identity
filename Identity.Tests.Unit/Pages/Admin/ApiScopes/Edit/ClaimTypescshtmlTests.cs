namespace Identity.Tests.Unit.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimTypescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var scope = new ApiScope { Id = 1, Name = "api1", UserClaims = [new ApiScopeClaim { Type = "sub" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new ClaimTypesModel(ctx.Object);
        var result = await model.OnGetAsync(1);
        Assert.IsType<PageResult>(result);
        Assert.Single(model.ClaimTypes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        Assert.IsType<NotFoundResult>(await new ClaimTypesModel(ctx.Object).OnGetAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewClaimType()
    {
        var scope = new ApiScope { Id = 1, Name = "api1", UserClaims = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ClaimTypesModel(ctx.Object)
        {
            ClaimTypes = [new ApiScopeClaim { Id = 0, Type = "sub" }],
        };
        var result = await model.OnPostAsync(1);
        Assert.Single(scope.UserClaims);
        Assert.Equal("sub", scope.UserClaims[0].Type);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/ApiScopes/Details/ClaimTypes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentClaimType()
    {
        var existing = new ApiScopeClaim { Id = 1, Type = "sub", ScopeId = 1 };
        var scope = new ApiScope { Id = 1, Name = "api1", UserClaims = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        await model.OnPostAsync(1);
        Assert.Empty(scope.UserClaims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var scope = new ApiScope { Id = 1, Name = "api1" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        var result = await model.OnPostAddRowAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.ClaimTypes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(99));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var scope = new ApiScope { Id = 1, Name = "api1" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [new ApiScopeClaim { Id = 1, Type = "sub" }] };
        var result = await model.OnPostRemoveRowAsync(1, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.ClaimTypes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(99, 0));
    }
}
