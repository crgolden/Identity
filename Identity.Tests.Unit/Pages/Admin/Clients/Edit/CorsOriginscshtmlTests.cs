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
public class CorsOriginscshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedCorsOrigins = [new ClientCorsOrigin { Id = ExistingEntityId, Origin = "https://example.com", ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

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
        var result = await model.OnGetAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewOrigin_WhenValid()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedCorsOrigins = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object)
        {
            CorsOrigins = [new ClientCorsOrigin { Id = 0, Origin = "https://new.com" }],
        };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyCorsOrigin = Assert.Single(client.AllowedCorsOrigins);
        Assert.Equal("https://new.com", onlyCorsOrigin.Origin);
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
        var result = await model.OnPostAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesOrigin_WhenNotPosted()
    {
        var existing = new ClientCorsOrigin { Id = ExistingEntityId, Origin = "https://old.com", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedCorsOrigins = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(client.AllowedCorsOrigins);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingCorsOrigin_WhenPostedWithId()
    {
        var existing = new ClientCorsOrigin { Id = ExistingEntityId, Origin = "https://old.com", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedCorsOrigins = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new CorsOriginsModel(ctx.Object)
        {
            CorsOrigins = [new ClientCorsOrigin { Id = ExistingEntityId, Origin = "https://new.com" }],
        };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("https://new.com", existing.Origin);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.CorsOrigins);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
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

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [new ClientCorsOrigin { Id = ExistingEntityId, Origin = "https://example.com" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.CorsOrigins);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new CorsOriginsModel(ctx.Object) { CorsOrigins = [] };
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}