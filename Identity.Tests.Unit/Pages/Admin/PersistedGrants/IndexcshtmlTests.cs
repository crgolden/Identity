namespace Identity.Tests.Unit.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PersistedGrants;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstSubjectAlphabetically = TestValues.NewFirstAlphabeticalName();
    private static readonly string LastSubjectAlphabetically = TestValues.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IPersistedGrantDbContext>();

        // Act
        var model = new IndexModel(ctx.Object);

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
                Key = TestValues.NewRequestId(),
                SubjectId = LastSubjectAlphabetically,
                ClientId = TestValues.NewClientIdentifier(),
            },
            new PersistedGrant
            {
                Key = TestValues.NewRequestId(),
                SubjectId = FirstSubjectAlphabetically,
                ClientId = TestValues.NewClientIdentifier(),
            },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.PersistedGrants.Count);
        Assert.Equal(FirstSubjectAlphabetically, model.PersistedGrants[0].SubjectId);
    }
}