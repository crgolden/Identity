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
public class CorsOriginscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedCorsOrigins = [new ClientCorsOrigin { Id = 1, Origin = "https://example.com", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.CorsOrigins);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewOrigin_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedCorsOrigins = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object)
        {
            CorsOrigins = [new ClientCorsOrigin { Id = 0, Origin = "https://new.com" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.AllowedCorsOrigins);
        Assert.Equal("https://new.com", client.AllowedCorsOrigins[0].Origin);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/CorsOrigins", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesOrigin_WhenNotPosted()
    {
        var existing = new ClientCorsOrigin { Id = 1, Origin = "https://old.com", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedCorsOrigins = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.AllowedCorsOrigins);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingCorsOrigin_WhenPostedWithId()
    {
        var existing = new ClientCorsOrigin { Id = 1, Origin = "https://old.com", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedCorsOrigins = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object)
        {
            CorsOrigins = [new ClientCorsOrigin { Id = 1, Origin = "https://new.com" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("https://new.com", existing.Origin);
    }
}
