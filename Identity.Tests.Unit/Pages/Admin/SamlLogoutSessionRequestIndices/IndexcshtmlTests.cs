namespace Identity.Tests.Unit.Pages.Admin.SamlLogoutSessionRequestIndices;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlLogoutSessionRequestIndices;
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
        Assert.IsType<PageModel>(new IndexModel(ctx.Object), exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSorted()
    {
        var lowerId = TestValues.NewEntityId();
        var data = new[]
        {
            new SamlLogoutSessionRequestIndex
            {
                Id = lowerId + 1,
                RequestId = TestValues.NewTokenFromFirstHalfOfAlphabet(9),
            },
            new SamlLogoutSessionRequestIndex
            {
                Id = lowerId,
                RequestId = TestValues.NewTokenFromSecondHalfOfAlphabet(9),
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessionRequestIndices).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(data.Length, model.SamlLogoutSessionRequestIndices.Count);
        Assert.Equal(lowerId, model.SamlLogoutSessionRequestIndices[0].Id);
    }
}