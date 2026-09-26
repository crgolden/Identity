namespace Identity.Tests.Unit.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ServerSideSessions;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string FirstSubjectId = Generated.NewFirstAlphabeticalName();

    private static readonly string LastSubjectId = Generated.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IPersistedGrantDbContext>();

        // Act
        var model = new Index(ctx.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSorted()
    {
        // Arrange
        var data = new[]
        {
            new ServerSideSession { Key = LastSubjectId, SubjectId = LastSubjectId },
            new ServerSideSession { Key = FirstSubjectId, SubjectId = FirstSubjectId },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.ServerSideSessions.Count);
        Assert.Equal(FirstSubjectId, model.ServerSideSessions[0].SubjectId);
    }
}
