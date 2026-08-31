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
public class RedirectUriscshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = "https://example.com/callback", ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

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
        var result = await model.OnGetAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewUri_WhenValid()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = 0, RedirectUri = "https://new.com/callback" }],
        };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyRedirectUri = Assert.Single(client.RedirectUris);
        Assert.Equal("https://new.com/callback", onlyRedirectUri.RedirectUri);
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
        var result = await model.OnPostAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesUri_WhenNotPosted()
    {
        var existing = new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = "https://old.com/callback", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(client.RedirectUris);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingRedirectUri_WhenPostedWithId()
    {
        var existing = new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = "https://old.com/callback", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = "https://new.com/callback" }],
        };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("https://new.com/callback", existing.RedirectUri);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.RedirectUris);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
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

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = "https://example.com/callback" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.RedirectUris);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}