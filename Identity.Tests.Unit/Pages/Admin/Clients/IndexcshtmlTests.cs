namespace Identity.Tests.Unit.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public void IsPageModel()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        Assert.IsAssignableFrom<PageModel>(new IndexModel(ctx.Object));
    }

    [Fact]
    public async Task OnGetAsync_ReturnsClientsOrderedByClientId()
    {
        var clients = new[]
        {
            new Client { Id = 1, ClientId = "beta" },
            new Client { Id = 2, ClientId = "alpha" },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(clients);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.Clients.Count);
        Assert.Equal("alpha", model.Clients[0].ClientId);
        Assert.Equal("beta", model.Clients[1].ClientId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsEmpty_WhenNoClients()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IndexModel(ctx.Object);
        await model.OnGetAsync();

        Assert.Empty(model.Clients);
    }
}
