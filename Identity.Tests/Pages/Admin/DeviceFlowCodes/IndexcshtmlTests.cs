namespace Identity.Tests.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.DeviceFlowCodes;
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
            new DeviceFlowCodes { DeviceCode = "b", ClientId = "z" },
            new DeviceFlowCodes { DeviceCode = "a", ClientId = "a" },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.DeviceFlowCodes).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.DeviceFlowCodes.Count);
        Assert.Equal("a", model.DeviceFlowCodes[0].ClientId);
    }
}
