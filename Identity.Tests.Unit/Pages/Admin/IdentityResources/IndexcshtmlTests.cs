namespace Identity.Tests.Unit.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityResources;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstResourceName = TestValues.NewFirstAlphabeticalName();

    private static readonly string LastResourceName = TestValues.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();

        // Act
        var model = new IndexModel(ctx.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        // Arrange
        var data = new[] { new IdentityResource { Name = LastResourceName }, new IdentityResource { Name = FirstResourceName } };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(FirstResourceName, model.IdentityResources[0].Name);
        Assert.Equal(LastResourceName, model.IdentityResources[1].Name);
    }
}