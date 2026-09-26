namespace Identity.Tests.Unit.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.DeviceFlowCodes;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string FirstClientIdAlphabetically =
        Generated.NewFirstAlphabeticalName();

    private static readonly string LastClientIdAlphabetically =
        Generated.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IPersistedGrantDbContext>();

        // Act
        var model = new Index(ctx.Object);

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
                DeviceCode = Generated.NewRequestId(),
                ClientId = LastClientIdAlphabetically,
            },
            new DeviceFlowCodes
            {
                DeviceCode = Generated.NewRequestId(),
                ClientId = FirstClientIdAlphabetically,
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.DeviceFlowCodes).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.DeviceFlowCodes.Count);
        Assert.Equal(FirstClientIdAlphabetically, model.DeviceFlowCodes[0].ClientId);
    }
}
