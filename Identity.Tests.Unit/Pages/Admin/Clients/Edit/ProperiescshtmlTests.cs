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
public class ProperiescshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [new ClientProperty { Id = ExistingEntityId, Key = "k", Value = "v", ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

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
        var result = await model.OnGetAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewProperty_WhenValid()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = 0, Key = "env", Value = "prod" }],
        };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyProperty = Assert.Single(client.Properties);
        Assert.Equal("env", onlyProperty.Key);
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
        var result = await model.OnPostAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesProperty_WhenNotPosted()
    {
        var existing = new ClientProperty { Id = ExistingEntityId, Key = "old", Value = "val", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(client.Properties);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingProperty_WhenPostedWithId()
    {
        var existing = new ClientProperty { Id = ExistingEntityId, Key = "env", Value = "staging", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = ExistingEntityId, Key = "env", Value = "prod" }],
        };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("prod", existing.Value);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [new ClientProperty { Id = ExistingEntityId, Key = "k", Value = "v" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Properties);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new PropertiesModel(ctx.Object) { Properties = [] };
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}