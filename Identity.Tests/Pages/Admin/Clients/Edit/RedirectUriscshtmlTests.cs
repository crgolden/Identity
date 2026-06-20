namespace Identity.Tests.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients.Edit;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RedirectUriscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", RedirectUris = [new ClientRedirectUri { Id = 1, RedirectUri = "https://example.com/callback", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.RedirectUris);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewUri_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", RedirectUris = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = 0, RedirectUri = "https://new.com/callback" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.RedirectUris);
        Assert.Equal("https://new.com/callback", client.RedirectUris[0].RedirectUri);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/RedirectUris", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesUri_WhenNotPosted()
    {
        var existing = new ClientRedirectUri { Id = 1, RedirectUri = "https://old.com/callback", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.RedirectUris);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingRedirectUri_WhenPostedWithId()
    {
        var existing = new ClientRedirectUri { Id = 1, RedirectUri = "https://old.com/callback", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = 1, RedirectUri = "https://new.com/callback" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("https://new.com/callback", existing.RedirectUri);
    }
}
