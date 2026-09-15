namespace Identity.Tests.Unit.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstScopeName = TestValues.NewFirstAlphabeticalName();

    private static readonly string LastScopeName = TestValues.NewLastAlphabeticalName();

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
        var data = new[] { new ApiScope { Name = LastScopeName }, new ApiScope { Name = FirstScopeName } };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(FirstScopeName, model.ApiScopes[0].Name);
        Assert.Equal(LastScopeName, model.ApiScopes[1].Name);
    }
}