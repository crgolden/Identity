namespace Identity.Tests.Unit.Pages.Admin.Keys;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Keys;
using Infrastructure;
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
    public async Task OnGetAsync_ReturnsSortedDescending()
    {
        var data = new[]
        {
            new Key { Id = "a", Created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Key { Id = "b", Created = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.Keys.Count);
        Assert.Equal("b", model.Keys[0].Id);
    }
}
