namespace Identity.Tests.Unit.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PushedAuthorizationRequests;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
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
        var expiringEarlier = Generated.NewUtcDateTime();
        var expiringEarlierId = Generated.NewEntityId();
        var data = new[]
        {
            new PushedAuthorizationRequest
            {
                Id = Generated.NewEntityId(),
                ExpiresAtUtc = expiringEarlier.AddDays(1),
            },
            new PushedAuthorizationRequest { Id = expiringEarlierId, ExpiresAtUtc = expiringEarlier },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.PushedAuthorizationRequests.Count);
        Assert.Equal(expiringEarlierId, model.PushedAuthorizationRequests[0].Id);
    }
}
