namespace Identity.Tests.Unit.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ServerSideSessions;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly string ExistingKey = TestValues.NewUserId().ToString();
    private static readonly string MissingKey = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var session = new ServerSideSession { Key = ExistingKey, SubjectId = "sub" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([session]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingKey);

        Assert.IsType<PageResult>(result);
        Assert.Equal("sub", model.ServerSideSession.SubjectId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ServerSideSession>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(MissingKey));
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        var session = new ServerSideSession { Key = ExistingKey, SubjectId = "sub" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([session]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnPostAsync(ExistingKey);

        ctx.Verify(c => c.ServerSideSessions.Remove(session), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ServerSideSession>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.ServerSideSessions).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingKey));
    }
}