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
public class EditcshtmlTests
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

        var model = new EditModel(ctx.Object);
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

        Assert.IsType<NotFoundResult>(await new EditModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = "google" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new EditModel(ctx.Object) { IdentityProvider = new IdentityProvider { Scheme = "google-updated", DisplayName = TestValues.NewClientName() } };
        var result = await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("google-updated", provider.Scheme);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);

        var model = new EditModel(ctx.Object) { IdentityProvider = new IdentityProvider { Scheme = "x" } };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }
}