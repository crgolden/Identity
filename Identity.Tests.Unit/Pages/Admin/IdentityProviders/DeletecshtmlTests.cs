namespace Identity.Tests.Unit.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityProviders;
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
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = "google" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("google", model.IdentityProvider.Scheme);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = "google" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnPostAsync(ExistingEntityId);

        ctx.Verify(c => c.IdentityProviders.Remove(provider), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnPostAsync(MissingEntityId));
    }
}