namespace Identity.Tests.Unit.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstEntityId = 'u' + TestValues.NewFirstAlphabeticalName();

    private static readonly string LastEntityId = 'u' + TestValues.NewLastAlphabeticalName();

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
    public async Task OnGetAsync_ReturnsSortedByEntityId()
    {
        // Arrange
        var data = new[]
        {
            new SamlServiceProvider { EntityId = LastEntityId },
            new SamlServiceProvider { EntityId = FirstEntityId },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.SamlServiceProviders.Count);
        Assert.Equal(FirstEntityId, model.SamlServiceProviders[0].EntityId);
    }
}