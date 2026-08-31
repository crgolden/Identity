namespace Identity.Tests.Unit.Pages.Admin.SamlLogoutSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlLogoutSessions.Details;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var session = new SamlLogoutSession { Id = ExistingEntityId, LogoutId = "logout1" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([session]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessions).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("logout1", model.SamlLogoutSession.LogoutId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlLogoutSession>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessions).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(MissingEntityId));
    }
}