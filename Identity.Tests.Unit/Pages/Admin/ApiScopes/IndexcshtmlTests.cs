namespace Identity.Tests.Unit.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstScopeName = TestValues.NewFirstAlphabeticalName();

    private static readonly string LastScopeName = TestValues.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        Assert.IsType<PageModel>(new IndexModel(ctx.Object), exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        var data = new[] { new ApiScope { Name = LastScopeName }, new ApiScope { Name = FirstScopeName } };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();
        Assert.Equal(FirstScopeName, model.ApiScopes[0].Name);
        Assert.Equal(LastScopeName, model.ApiScopes[1].Name);
    }
}