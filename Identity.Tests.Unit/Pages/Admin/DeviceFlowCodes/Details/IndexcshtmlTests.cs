namespace Identity.Tests.Unit.Pages.Admin.DeviceFlowCodes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.DeviceFlowCodes.Details;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string ExistingClientId = TestValues.NewClientIdentifier();

    private static readonly string ExistingKey = TestValues.NewUserId().ToString();
    private static readonly string MissingKey = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var code = new DeviceFlowCodes { DeviceCode = ExistingKey, ClientId = ExistingClientId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([code]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.DeviceFlowCodes).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingKey);

        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingClientId, model.DeviceFlowCode.ClientId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<DeviceFlowCodes>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.DeviceFlowCodes).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(MissingKey));
    }
}