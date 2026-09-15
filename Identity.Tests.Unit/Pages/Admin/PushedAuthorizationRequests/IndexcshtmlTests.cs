namespace Identity.Tests.Unit.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PushedAuthorizationRequests;
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
        var expiringEarlier = TestValues.NewUtcDateTime();
        var expiringEarlierId = TestValues.NewEntityId();
        var data = new[]
        {
            new PushedAuthorizationRequest
            {
                Id = TestValues.NewEntityId(),
                ExpiresAtUtc = expiringEarlier.AddDays(1),
            },
            new PushedAuthorizationRequest { Id = expiringEarlierId, ExpiresAtUtc = expiringEarlier },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.PushedAuthorizationRequests.Count);
        Assert.Equal(expiringEarlierId, model.PushedAuthorizationRequests[0].Id);
    }
}