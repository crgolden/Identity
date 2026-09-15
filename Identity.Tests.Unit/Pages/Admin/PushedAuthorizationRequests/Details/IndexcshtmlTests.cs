namespace Identity.Tests.Unit.Pages.Admin.PushedAuthorizationRequests.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PushedAuthorizationRequests.Details;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string ReferenceValueHash = TestValues.NewReferenceValueHash();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var par = new PushedAuthorizationRequest { Id = ExistingEntityId, ReferenceValueHash = ReferenceValueHash };
        var mockSet = MockDbSetHelper.BuildMockDbSet([par]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ReferenceValueHash, model.PushedAuthorizationRequest.ReferenceValueHash);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<PushedAuthorizationRequest>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}