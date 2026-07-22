namespace Identity.Tests.Unit.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiResources.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ScopescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api", Scopes = [new ApiResourceScope { Id = 1, Scope = "my-api.read" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new ScopesModel(ctx.Object).OnGetAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewScope()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api", Scopes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object) { Scopes = [new ApiResourceScope { Id = 0, Scope = "my-api.write" }] };
        var result = await model.OnPostAsync(1);

        Assert.Single(resource.Scopes);
        Assert.Equal("my-api.write", resource.Scopes[0].Scope);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/ApiResources/Details/Scopes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentScope()
    {
        var existing = new ApiResourceScope { Id = 1, Scope = "my-api.read", ApiResourceId = 1 };
        var resource = new ApiResource { Id = 1, Name = "my-api", Scopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        await model.OnPostAsync(1);

        Assert.Empty(resource.Scopes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostAddRowAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(99));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [new ApiResourceScope { Id = 1, Scope = "my-api.read" }] };
        var result = await model.OnPostRemoveRowAsync(1, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Scopes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(99, 0));
    }
}
