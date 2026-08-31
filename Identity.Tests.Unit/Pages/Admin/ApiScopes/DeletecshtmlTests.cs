namespace Identity.Tests.Unit.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var scope = new ApiScope { Id = ExistingEntityId, Name = "api1" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new DeleteModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);
        Assert.IsType<PageResult>(result);
        Assert.Equal("api1", model.Scope.Name);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenFound()
    {
        var scope = new ApiScope { Id = ExistingEntityId, Name = "api1" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var result = await new DeleteModel(ctx.Object).OnPostAsync(ExistingEntityId);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnPostAsync(MissingEntityId));
    }
}