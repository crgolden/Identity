namespace Identity.Tests.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var sp = new SamlServiceProvider { Id = 1, EntityId = "urn:sp" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Equal("urn:sp", model.SamlServiceProvider.EntityId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnGetAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        var sp = new SamlServiceProvider { Id = 1, EntityId = "urn:sp" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new DeleteModel(ctx.Object);
        var result = await model.OnPostAsync(1);

        ctx.Verify(c => c.SamlServiceProviders.Remove(sp), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new DeleteModel(ctx.Object).OnPostAsync(99));
    }
}
