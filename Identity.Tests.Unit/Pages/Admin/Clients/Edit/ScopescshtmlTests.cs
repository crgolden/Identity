namespace Identity.Tests.Unit.Pages.Admin.Clients.Edit;

using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ScopescshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedScopes = [new ClientScope { Id = ExistingEntityId, Scope = IdentityServerConstants.StandardScopes.OpenId, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewScope_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedScopes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ScopesModel(ctx.Object)
        {
            Scopes = [new ClientScope { Id = 0, Scope = IdentityServerConstants.StandardScopes.Profile }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyAllowedScope = Assert.Single(client.AllowedScopes);
        Assert.Equal(IdentityServerConstants.StandardScopes.Profile, onlyAllowedScope.Scope);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ScopesModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object) { Scopes = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesScope_WhenNotPosted()
    {
        // Arrange
        var existing = new ClientScope { Id = ExistingEntityId, Scope = IdentityServerConstants.StandardScopes.OpenId, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedScopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ScopesModel(ctx.Object) { Scopes = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.AllowedScopes);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingScope_WhenPostedWithId()
    {
        // Arrange
        var existing = new ClientScope { Id = ExistingEntityId, Scope = IdentityServerConstants.StandardScopes.OpenId, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), AllowedScopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ScopesModel(ctx.Object)
        {
            Scopes = [new ClientScope { Id = ExistingEntityId, Scope = IdentityServerConstants.StandardScopes.Profile }],
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(IdentityServerConstants.StandardScopes.Profile, existing.Scope);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object) { Scopes = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scopes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object) { Scopes = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object) { Scopes = [new ClientScope { Id = ExistingEntityId, Scope = IdentityServerConstants.StandardScopes.OpenId }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Scopes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new ScopesModel(ctx.Object) { Scopes = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}