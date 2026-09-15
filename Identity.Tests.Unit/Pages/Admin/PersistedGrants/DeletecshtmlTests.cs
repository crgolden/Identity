namespace Identity.Tests.Unit.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PersistedGrants;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly string GrantSubjectId = TestValues.NewSubjectId();

    private static readonly string ExistingKey = TestValues.NewUserId().ToString();
    private static readonly string MissingKey = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var grant = new PersistedGrant { Key = ExistingKey, SubjectId = GrantSubjectId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([grant]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new DeleteModel(ctx.Object);

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
        var model = new DeleteModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        // Arrange
        var grant = new PersistedGrant { Key = ExistingKey };
        var mockSet = MockDbSetHelper.BuildMockDbSet([grant]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new DeleteModel(ctx.Object);

        // Act
        var result = await model.OnPostAsync(ExistingKey);

        // Assert
        ctx.Verify(c => c.PersistedGrants.Remove(grant), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingIndex, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<PersistedGrant>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PersistedGrants).Returns(mockSet.Object);
        var model = new DeleteModel(ctx.Object);

        // Act
        var result = await model.OnPostAsync(MissingKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}