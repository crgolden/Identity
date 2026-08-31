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
public class ProperiescshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [new ApiResourceProperty { Id = ExistingEntityId, Key = "k", Value = "v" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new PropertiesModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewProperty()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object) { Properties = [new ApiResourceProperty { Id = 0, Key = "env", Value = "prod" }] };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyProperty = Assert.Single(resource.Properties);
        Assert.Equal("env", onlyProperty.Key);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/ApiResources/Details/Properties", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentProperty()
    {
        var existing = new ApiResourceProperty { Id = ExistingEntityId, Key = "old", Value = "val", ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(resource.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [new ApiResourceProperty { Id = ExistingEntityId, Key = "k", Value = "v" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Properties);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingEntityId, 0));
    }
}