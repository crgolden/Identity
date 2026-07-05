namespace Identity.Tests.Unit.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GrantTypescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedGrantTypes = [new ClientGrantType { Id = 1, GrantType = "authorization_code", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.GrantTypes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewGrantType_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", AllowedGrantTypes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object)
        {
            GrantTypes = [new ClientGrantType { Id = 0, GrantType = "client_credentials" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.AllowedGrantTypes);
        Assert.Equal("client_credentials", client.AllowedGrantTypes[0].GrantType);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/GrantTypes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesGrantType_WhenNotPosted()
    {
        var existing = new ClientGrantType { Id = 1, GrantType = "implicit", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedGrantTypes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.AllowedGrantTypes);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingGrantType_WhenPostedWithId()
    {
        var existing = new ClientGrantType { Id = 1, GrantType = "implicit", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", AllowedGrantTypes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object)
        {
            GrantTypes = [new ClientGrantType { Id = 1, GrantType = "authorization_code" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("authorization_code", existing.GrantType);
    }
}
