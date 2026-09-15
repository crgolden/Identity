namespace Identity.Tests.Unit.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DetailscshtmlTests
{
    private static readonly string EntityId = TestValues.NewEntityIdUrn();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var sp = new SamlServiceProvider { Id = ExistingEntityId, EntityId = EntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        var model = new DetailsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(EntityId, model.SamlServiceProvider.EntityId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        var model = new DetailsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}