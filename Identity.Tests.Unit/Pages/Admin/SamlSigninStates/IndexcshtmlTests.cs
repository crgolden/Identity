namespace Identity.Tests.Unit.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlSigninStates;
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
            new SamlSigninState { Id = 1, ExpiresAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SamlSigninState { Id = 2, ExpiresAtUtc = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.SamlSigninStates.Count);
        Assert.Equal(2, model.SamlSigninStates[0].Id);
    }
}
