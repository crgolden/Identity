namespace Identity.Tests.Unit.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.PushedAuthorizationRequests;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var par = new PushedAuthorizationRequest { Id = ExistingEntityId, ReferenceValueHash = "hash" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([par]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("hash", model.PushedAuthorizationRequest.ReferenceValueHash);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<PushedAuthorizationRequest>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        var par = new PushedAuthorizationRequest { Id = ExistingEntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([par]);
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnPostAsync(ExistingEntityId);

        ctx.Verify(c => c.PushedAuthorizationRequests.Remove(par), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<PushedAuthorizationRequest>());
        var ctx = new Mock<IPersistedGrantDbContext>();
        ctx.Setup(c => c.PushedAuthorizationRequests).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }
}