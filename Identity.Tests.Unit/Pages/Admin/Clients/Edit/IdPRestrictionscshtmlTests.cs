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
    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var client = new Client { Id = 1, ClientId = "test", IdentityProviderRestrictions = [new ClientIdPRestriction { Id = 1, Provider = "Google", ClientId = 1 }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);

        var model = new IdPRestrictionsModel(ctx.Object);
        var result = await model.OnGetAsync(1);

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
        var result = await model.OnGetAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewRestriction_WhenValid()
    {
        var client = new Client { Id = 1, ClientId = "test", IdentityProviderRestrictions = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object)
        {
            IdPRestrictions = [new ClientIdPRestriction { Id = 0, Provider = "Facebook" }],
        };
        var result = await model.OnPostAsync(1);

        Assert.Single(client.IdentityProviderRestrictions);
        Assert.Equal("Facebook", client.IdentityProviderRestrictions[0].Provider);
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
        var result = await model.OnPostAsync(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesRestriction_WhenNotPosted()
    {
        var existing = new ClientIdPRestriction { Id = 1, Provider = "Google", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", IdentityProviderRestrictions = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object) { IdPRestrictions = [] };
        await model.OnPostAsync(1);

        Assert.Empty(client.IdentityProviderRestrictions);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingIdPRestriction_WhenPostedWithId()
    {
        var existing = new ClientIdPRestriction { Id = 1, Provider = "Google", ClientId = 1 };
        var client = new Client { Id = 1, ClientId = "test", IdentityProviderRestrictions = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new IdPRestrictionsModel(ctx.Object)
        {
            IdPRestrictions = [new ClientIdPRestriction { Id = 1, Provider = "Facebook" }],
        };
        await model.OnPostAsync(1);

        Assert.Equal("Facebook", existing.Provider);
    }
}
