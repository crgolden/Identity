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
public class SecretscshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", ClientSecrets = [new ClientSecret { Id = 1, Value = "hashed", Type = "SharedSecret", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Secrets);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewSecret_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", ClientSecrets = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new SecretsModel(ctx.Object)
        {
            Secrets = [new ClientSecret { Id = 0, Value = "secret123", Type = "SharedSecret" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.ClientSecrets);
        Assert.Equal("secret123", client.ClientSecrets[0].Value);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/Secrets", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingSecret_WhenPostedWithId()
    {
        var existing = new ClientSecret { Id = 1, Value = "hashed", Type = "SharedSecret", Description = "old", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new SecretsModel(ctx.Object)
        {
            Secrets = [new ClientSecret { Id = 1, Type = "SharedSecret", Description = "updated" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("updated", existing.Description);
        Assert.Equal("hashed", existing.Value);
    }

    [Fact]
    public async Task OnPostAsync_RemovesSecret_WhenNotPosted()
    {
        var existing = new ClientSecret { Id = 1, Value = "hashed", Type = "SharedSecret", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new SecretsModel(ctx.Object) { Secrets = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.ClientSecrets);
    }
}
