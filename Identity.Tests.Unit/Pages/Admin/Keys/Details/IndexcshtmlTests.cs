namespace Identity.Tests.Unit.Pages.Admin.Keys.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Keys.Details;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string AlgorithmName = TestValues.NewSigningAlgorithmName();

    private static readonly string ExistingKey = TestValues.NewUserId().ToString();
    private static readonly string MissingKey = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var key = new Key { Id = ExistingKey, Algorithm = AlgorithmName };
        var mockSet = MockDbSetHelper.BuildMockDbSet([key]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingKey);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(AlgorithmName, model.Key.Algorithm);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Key>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.Keys).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}