namespace Identity.Tests.Unit.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.DeviceFlowCodes;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstClientIdAlphabetically =
        TestValues.NewTokenFromFirstHalfOfAlphabet(9);

    private static readonly string LastClientIdAlphabetically =
        TestValues.NewTokenFromSecondHalfOfAlphabet(9);

    [Fact]
    public void IsPageModel()
    {
        var ctx = new Mock<IPersistedGrantDbContext>();
        Assert.IsType<PageModel>(new IndexModel(ctx.Object), exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSorted()
    {
        var data = new[]
        {
            new DeviceFlowCodes
            {
                DeviceCode = TestValues.NewRequestId(),
                ClientId = LastClientIdAlphabetically,
            },
            new DeviceFlowCodes
            {
                DeviceCode = TestValues.NewRequestId(),
                ClientId = FirstClientIdAlphabetically,
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.DeviceFlowCodes).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(data.Length, model.DeviceFlowCodes.Count);
        Assert.Equal(FirstClientIdAlphabetically, model.DeviceFlowCodes[0].ClientId);
    }
}