namespace Identity.Tests.Unit.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PostLogoutRedirectUriscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = 1, PostLogoutRedirectUri = "https://example.com/logout", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PostLogoutRedirectUrisModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PostLogoutRedirectUrisModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewUri_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", PostLogoutRedirectUris = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PostLogoutRedirectUrisModel(ctx.Object)
        {
            PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = 0, PostLogoutRedirectUri = "https://new.com/logout" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.PostLogoutRedirectUris);
        Assert.Equal("https://new.com/logout", client.PostLogoutRedirectUris[0].PostLogoutRedirectUri);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/PostLogoutRedirectUris", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesUri_WhenNotPosted()
    {
        var existing = new ClientPostLogoutRedirectUri { Id = 1, PostLogoutRedirectUri = "https://old.com/logout", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", PostLogoutRedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingPostLogoutRedirectUri_WhenPostedWithId()
    {
        var existing = new ClientPostLogoutRedirectUri { Id = 1, PostLogoutRedirectUri = "https://old.com/logout", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", PostLogoutRedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PostLogoutRedirectUrisModel(ctx.Object)
        {
            PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = 1, PostLogoutRedirectUri = "https://new.com/logout" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("https://new.com/logout", existing.PostLogoutRedirectUri);
    }
}
