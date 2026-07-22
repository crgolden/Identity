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
public class ScopescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedScopes = [new ClientScope { Id = 1, Scope = "openid", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewScope_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedScopes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object)
        {
            Scopes = [new ClientScope { Id = 0, Scope = "profile" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.AllowedScopes);
        Assert.Equal("profile", client.AllowedScopes[0].Scope);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/Scopes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesScope_WhenNotPosted()
    {
        var existing = new ClientScope { Id = 1, Scope = "openid", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedScopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.AllowedScopes);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingScope_WhenPostedWithId()
    {
        var existing = new ClientScope { Id = 1, Scope = "openid", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedScopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ScopesModel(ctx.Object)
        {
            Scopes = [new ClientScope { Id = 1, Scope = "profile" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("profile", existing.Scope);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostAddRowAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostAddRowAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var client = new Client { Id = 1, ClientId = "test" };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [new ClientScope { Id = 1, Scope = "openid" }] };
        var result = await model.OnPostRemoveRowAsync(1, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Scopes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new ScopesModel(ctx.Object) { Scopes = [] };
        var result = await model.OnPostRemoveRowAsync(99, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}
