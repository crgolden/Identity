namespace Identity.Tests.Unit.Pages.Admin.Keys;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Keys;
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
        var newerCreated = TestValues.NewUtcInstant();
        var olderCreated = TestValues.NewUtcInstantBefore(newerCreated);
        var olderKeyId = TestValues.NewKeyId();
        var newerKeyId = TestValues.NewKeyId();
        var data = new[]
        {
            new Key { Id = olderKeyId, Created = olderCreated.UtcDateTime },
            new Key { Id = newerKeyId, Created = newerCreated.UtcDateTime },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.Keys.Count);
        Assert.Equal(newerKeyId, model.Keys[0].Id);
    }
}