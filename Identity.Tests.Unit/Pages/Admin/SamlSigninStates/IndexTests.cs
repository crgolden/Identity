namespace Identity.Tests.Unit.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlSigninStates;
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
    public async Task OnGetAsync_ReturnsSortedDescending()
    {
        // Arrange
        var expiringEarlier = Generated.NewUtcDateTime();
        var expiringLaterId = Generated.NewEntityId();
        var data = new[]
        {
            new SamlSigninState { Id = Generated.NewEntityId(), ExpiresAtUtc = expiringEarlier },
            new SamlSigninState { Id = expiringLaterId, ExpiresAtUtc = expiringEarlier.AddDays(1) },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.SamlSigninStates.Count);
        Assert.Equal(expiringLaterId, model.SamlSigninStates[0].Id);
    }
}
