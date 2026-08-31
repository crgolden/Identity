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
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Scopes = [new ApiResourceScope { Id = ExistingEntityId, Scope = "my-api.read" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new ScopesModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewScope()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Scopes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object) { Scopes = [new ApiResourceScope { Id = 0, Scope = "my-api.write" }] };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyScope = Assert.Single(resource.Scopes);
        Assert.Equal("my-api.write", onlyScope.Scope);
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
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentScope()
    {
        var existing = new ApiResourceScope { Id = ExistingEntityId, Scope = "my-api.read", ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Scopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(resource.Scopes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

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
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [new ApiResourceScope { Id = ExistingEntityId, Scope = "my-api.read" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

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
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingEntityId, 0));
    }
}