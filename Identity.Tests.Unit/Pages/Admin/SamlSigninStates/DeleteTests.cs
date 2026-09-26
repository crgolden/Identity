namespace Identity.Tests.Unit.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlSigninStates;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeleteTests
{
    private static readonly string ServiceProviderEntityId = Generated.NewNamespacedEntityId();

    private static readonly int ExistingEntityId = Generated.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var state = new SamlSigninState { Id = ExistingEntityId, ServiceProviderEntityId = ServiceProviderEntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([state]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);
        var model = new Delete(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ServiceProviderEntityId, model.SamlSigninState.ServiceProviderEntityId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlSigninState>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);
        var model = new Delete(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        // Arrange
        var state = new SamlSigninState { Id = ExistingEntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([state]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Delete(ctx.Object);

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        ctx.Verify(c => c.SamlSigninStates.Remove(state), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingIndex, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlSigninState>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.SamlSigninStates).Returns(mockSet.Object);
        var model = new Delete(ctx.Object);

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
