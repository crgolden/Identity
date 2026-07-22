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
public class SecretscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api", Secrets = [new ApiResourceSecret { Id = 1, Description = "prod" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Secrets);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new SecretsModel(ctx.Object).OnGetAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewSecret()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api", Secrets = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new SecretsModel(ctx.Object) { Secrets = [new ApiResourceSecret { Id = 0, Description = "new", Value = "secret", Type = "SharedSecret" }] };
        var result = await model.OnPostAsync(1);

        Assert.Single(resource.Secrets);
        Assert.Equal("new", resource.Secrets[0].Description);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/ApiResources/Details/Secrets", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentSecret()
    {
        var existing = new ApiResourceSecret { Id = 1, Description = "old", ApiResourceId = 1 };
        var resource = new ApiResource { Id = 1, Name = "my-api", Secrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        await model.OnPostAsync(1);

        Assert.Empty(resource.Secrets);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRowWithDefaultType_WhenFound()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        var result = await model.OnPostAddRowAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Secrets);
        Assert.Equal("SharedSecret", model.Secrets[0].Type);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(99));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var resource = new ApiResource { Id = 1, Name = "my-api" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [new ApiResourceSecret { Id = 1, Description = "prod" }] };
        var result = await model.OnPostRemoveRowAsync(1, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Secrets);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(99, 0));
    }
}
