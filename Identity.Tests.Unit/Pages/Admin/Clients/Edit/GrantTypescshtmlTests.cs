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
public class GrantTypescshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedGrantTypes = [new ClientGrantType { Id = ExistingEntityId, GrantType = "authorization_code", ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

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
        var result = await model.OnGetAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewGrantType_WhenValid()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedGrantTypes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object)
        {
            GrantTypes = [new ClientGrantType { Id = 0, GrantType = "client_credentials" }],
        };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyGrantType = Assert.Single(client.AllowedGrantTypes);
        Assert.Equal("client_credentials", onlyGrantType.GrantType);
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
        var result = await model.OnPostAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesGrantType_WhenNotPosted()
    {
        var existing = new ClientGrantType { Id = ExistingEntityId, GrantType = "implicit", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedGrantTypes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(client.AllowedGrantTypes);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingGrantType_WhenPostedWithId()
    {
        var existing = new ClientGrantType { Id = ExistingEntityId, GrantType = "implicit", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedGrantTypes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new GrantTypesModel(ctx.Object)
        {
            GrantTypes = [new ClientGrantType { Id = ExistingEntityId, GrantType = "authorization_code" }],
        };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("authorization_code", existing.GrantType);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.GrantTypes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
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

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [new ClientGrantType { Id = ExistingEntityId, GrantType = "implicit" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.GrantTypes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new GrantTypesModel(ctx.Object) { GrantTypes = [] };
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}