namespace Identity.Tests.Unit.Pages.Admin.PersistedGrants.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PersistedGrants.Details;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string GrantSubjectId = Generated.NewSubjectId();

    private static readonly string ExistingKey = Generated.NewUserId().ToString();
    private static readonly string MissingKey = Generated.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var grant = new PersistedGrant { Key = ExistingKey, SubjectId = GrantSubjectId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([grant]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingKey);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(GrantSubjectId, model.PersistedGrant.SubjectId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<PersistedGrant>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
