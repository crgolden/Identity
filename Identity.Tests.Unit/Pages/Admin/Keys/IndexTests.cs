namespace Identity.Tests.Unit.Pages.Admin.Keys;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Keys;
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
        var newerCreated = Generated.NewUtcInstant();
        var olderCreated = Generated.NewUtcInstantBefore(newerCreated);
        var olderKeyId = Generated.NewKeyId();
        var newerKeyId = Generated.NewKeyId();
        var data = new[]
        {
            new Key { Id = olderKeyId, Created = olderCreated.UtcDateTime },
            new Key { Id = newerKeyId, Created = newerCreated.UtcDateTime },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.Keys.Count);
        Assert.Equal(newerKeyId, model.Keys[0].Id);
    }
}
