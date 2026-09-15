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
        TestValues.NewFirstAlphabeticalName();

    private static readonly string LastClientIdAlphabetically =
        TestValues.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IPersistedGrantDbContext>();

        // Act
        var model = new IndexModel(ctx.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSorted()
    {
        // Arrange
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

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.DeviceFlowCodes.Count);
        Assert.Equal(FirstClientIdAlphabetically, model.DeviceFlowCodes[0].ClientId);
    }
}