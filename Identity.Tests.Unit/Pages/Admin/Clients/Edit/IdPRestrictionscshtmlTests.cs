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
public class IdPRestrictionscshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), IdentityProviderRestrictions = [new ClientIdPRestriction { Id = ExistingEntityId, Provider = "Google", ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.IdPRestrictions);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object);
        var result = await model.OnGetAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewRestriction_WhenValid()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), IdentityProviderRestrictions = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object)
        {
            IdPRestrictions = [new ClientIdPRestriction { Id = 0, Provider = "Facebook" }],
        };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyProviderRestriction = Assert.Single(client.IdentityProviderRestrictions);
        Assert.Equal("Facebook", onlyProviderRestriction.Provider);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Clients/Details/IdPRestrictions", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
        var result = await model.OnPostAsync(MissingEntityId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesRestriction_WhenNotPosted()
    {
        var existing = new ClientIdPRestriction { Id = ExistingEntityId, Provider = "Google", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), IdentityProviderRestrictions = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(client.IdentityProviderRestrictions);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingIdPRestriction_WhenPostedWithId()
    {
        var existing = new ClientIdPRestriction { Id = ExistingEntityId, Provider = "Google", ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), IdentityProviderRestrictions = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object)
        {
            IdPRestrictions = [new ClientIdPRestriction { Id = ExistingEntityId, Provider = "Facebook" }],
        };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Equal("Facebook", existing.Provider);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.IdPRestrictions);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
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

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [new ClientIdPRestriction { Id = ExistingEntityId, Provider = "Google" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.IdPRestrictions);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        Assert.IsType<NotFoundResult>(result);
    }
}