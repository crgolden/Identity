namespace Identity.Tests.Unit.Pages.Admin.SamlLogoutSessionRequestIndices;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlLogoutSessionRequestIndices;
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
        var lowerId = Generated.NewEntityId();
        var data = new[]
        {
            new SamlLogoutSessionRequestIndex
            {
                Id = lowerId + 1,
                RequestId = Generated.NewFirstAlphabeticalName(),
            },
            new SamlLogoutSessionRequestIndex
            {
                Id = lowerId,
                RequestId = Generated.NewLastAlphabeticalName(),
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlLogoutSessionRequestIndices).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.SamlLogoutSessionRequestIndices.Count);
        Assert.Equal(lowerId, model.SamlLogoutSessionRequestIndices[0].Id);
    }
}
