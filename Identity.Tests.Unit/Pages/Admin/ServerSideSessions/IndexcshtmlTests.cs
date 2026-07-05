namespace Identity.Tests.Unit.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ServerSideSessions;
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
            new ServerSideSession { Key = "b", SubjectId = "b" },
            new ServerSideSession { Key = "a", SubjectId = "a" },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.ServerSideSessions.Count);
        Assert.Equal("a", model.ServerSideSessions[0].SubjectId);
    }
}
