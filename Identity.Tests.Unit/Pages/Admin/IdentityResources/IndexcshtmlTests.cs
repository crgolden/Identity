namespace Identity.Tests.Unit.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityResources;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public void IsPageModel()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        Assert.IsAssignableFrom<PageModel>(new IndexModel(ctx.Object));
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        var data = new[] { new IdentityResource { Name = "z" }, new IdentityResource { Name = "a" } };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();
        Assert.Equal("a", model.IdentityResources[0].Name);
        Assert.Equal("z", model.IdentityResources[1].Name);
    }
}
