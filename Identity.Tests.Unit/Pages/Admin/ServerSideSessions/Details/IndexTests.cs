namespace Identity.Tests.Unit.Pages.Admin.ServerSideSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ServerSideSessions.Details;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string SessionSubjectId = Generated.NewSubjectId();

    private static readonly string ExistingKey = Generated.NewUserId().ToString();
    private static readonly string MissingKey = Generated.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var session = new ServerSideSession { Key = ExistingKey, SubjectId = SessionSubjectId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([session]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingKey);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(SessionSubjectId, model.ServerSideSession.SubjectId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ServerSideSession>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
