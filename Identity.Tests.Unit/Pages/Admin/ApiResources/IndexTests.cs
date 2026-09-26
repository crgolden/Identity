namespace Identity.Tests.Unit.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiResources;
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
        var ctx = new Mock<IConfigurationDbContext>();

        // Act
        var model = new Index(ctx.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        // Arrange
        var firstAlphabetically = Generated.NewFirstAlphabeticalName();
        var lastAlphabetically = Generated.NewLastAlphabeticalName();
        var data = new[]
        {
            new ApiResource { Name = lastAlphabetically },
            new ApiResource { Name = firstAlphabetically },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.ApiResources.Count);
        Assert.Equal(firstAlphabetically, model.ApiResources[0].Name);
    }
}
