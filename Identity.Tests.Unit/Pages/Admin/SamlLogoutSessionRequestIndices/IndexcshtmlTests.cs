namespace Identity.Tests.Unit.Pages.Admin.SamlLogoutSessionRequestIndices;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlLogoutSessionRequestIndices;
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
        var ctx = new Mock<IPersistedGrantDbContext>();
        Assert.IsAssignableFrom<PageModel>(new IndexModel(ctx.Object));
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSorted()
    {
        var data = new[]
        {
            new SamlLogoutSessionRequestIndex { Id = 2, RequestId = "r2" },
            new SamlLogoutSessionRequestIndex { Id = 1, RequestId = "r1" },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessionRequestIndices).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.SamlLogoutSessionRequestIndices.Count);
        Assert.Equal(1, model.SamlLogoutSessionRequestIndices[0].Id);
    }
}
