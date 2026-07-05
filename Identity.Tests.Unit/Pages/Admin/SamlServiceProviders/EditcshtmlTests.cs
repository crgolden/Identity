namespace Identity.Tests.Unit.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EditcshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var sp = new SamlServiceProvider { Id = 1, EntityId = "urn:sp" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        var model = new EditModel(ctx.Object);
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

        Assert.IsType<NotFoundResult>(await new EditModel(ctx.Object).OnGetAsync(99));
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        var sp = new SamlServiceProvider { Id = 1, EntityId = "urn:sp" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new EditModel(ctx.Object) { SamlServiceProvider = new SamlServiceProvider { EntityId = "urn:sp-updated", Enabled = true } };
        var result = await model.OnPostAsync(1);

        Assert.Equal("urn:sp-updated", sp.EntityId);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        var model = new EditModel(ctx.Object) { SamlServiceProvider = new SamlServiceProvider { EntityId = "urn:x" } };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(99));
    }
}
