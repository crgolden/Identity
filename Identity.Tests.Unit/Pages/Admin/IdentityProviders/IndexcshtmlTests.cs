namespace Identity.Tests.Unit.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstSchemeName = TestValues.NewFirstAlphabeticalName();

    private static readonly string LastSchemeName = TestValues.NewLastAlphabeticalName();

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
    public async Task OnGetAsync_ReturnsSortedByScheme()
    {
        // Arrange
        var data = new[]
        {
            new IdentityProvider { Scheme = LastSchemeName },
            new IdentityProvider { Scheme = FirstSchemeName },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.IdentityProviders.Count);
        Assert.Equal(FirstSchemeName, model.IdentityProviders[0].Scheme);
    }
}