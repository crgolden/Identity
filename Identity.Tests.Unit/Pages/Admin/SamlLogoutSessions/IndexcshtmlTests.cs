namespace Identity.Tests.Unit.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlLogoutSessions;
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
    public async Task OnGetAsync_ReturnsSortedDescending()
    {
        // Arrange
        var expiringEarlier = TestValues.NewUtcDateTime();
        var expiringLaterId = TestValues.NewEntityId();
        var data = new[]
        {
            new SamlLogoutSession { Id = TestValues.NewEntityId(), ExpiresAtUtc = expiringEarlier },
            new SamlLogoutSession { Id = expiringLaterId, ExpiresAtUtc = expiringEarlier.AddDays(1) },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessions).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.SamlLogoutSessions.Count);
        Assert.Equal(expiringLaterId, model.SamlLogoutSessions[0].Id);
    }
}