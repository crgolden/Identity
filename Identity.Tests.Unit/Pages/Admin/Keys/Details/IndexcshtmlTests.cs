namespace Identity.Tests.Unit.Pages.Admin.Keys.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Keys.Details;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string ExistingKey = TestValues.NewUserId().ToString();
    private static readonly string MissingKey = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var key = new Key { Id = ExistingKey, Algorithm = "RS256" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([key]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingKey);

        Assert.IsType<PageResult>(result);
        Assert.Equal("RS256", model.Key.Algorithm);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Key>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(MissingKey));
    }
}