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
public class ProperiescshtmlTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", Properties = [new ClientProperty { Id = 1, Key = "k", Value = "v", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object);
        var result = await model.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object);
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewProperty_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", Properties = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = 0, Key = "env", Value = "prod" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.Properties);
        Assert.Equal("env", client.Properties[0].Key);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/Properties", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesProperty_WhenNotPosted()
    {
        var existing = new ClientProperty { Id = 1, Key = "old", Value = "val", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.Properties);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingProperty_WhenPostedWithId()
    {
        var existing = new ClientProperty { Id = 1, Key = "env", Value = "staging", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = 1, Key = "env", Value = "prod" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("prod", existing.Value);
    }
}
