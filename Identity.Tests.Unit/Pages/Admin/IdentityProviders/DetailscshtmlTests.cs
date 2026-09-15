namespace Identity.Tests.Unit.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DetailscshtmlTests
{
    private static readonly string SchemeName = TestValues.NewSchemeName();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = SchemeName };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new DetailsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(SchemeName, model.IdentityProvider.Scheme);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new DetailsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}