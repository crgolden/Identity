namespace Identity.Tests.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PushedAuthorizationRequests;
using Identity.Tests.Infrastructure;
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
            new PushedAuthorizationRequest { Id = 2, ExpiresAtUtc = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PushedAuthorizationRequest { Id = 1, ExpiresAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.PushedAuthorizationRequests.Count);
        Assert.Equal(1, model.PushedAuthorizationRequests[0].Id);
    }
}
