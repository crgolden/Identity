namespace Identity.Tests.Unit.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PersistedGrants;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string FirstSubjectAlphabetically = Generated.NewFirstAlphabeticalName();
    private static readonly string LastSubjectAlphabetically = Generated.NewLastAlphabeticalName();

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
            new PersistedGrant
            {
                Key = Generated.NewRequestId(),
                SubjectId = LastSubjectAlphabetically,
                ClientId = Generated.NewClientIdentifier(),
            },
            new PersistedGrant
            {
                Key = Generated.NewRequestId(),
                SubjectId = FirstSubjectAlphabetically,
                ClientId = Generated.NewClientIdentifier(),
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new Index(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.PersistedGrants.Count);
        Assert.Equal(FirstSubjectAlphabetically, model.PersistedGrants[0].SubjectId);
    }
}
