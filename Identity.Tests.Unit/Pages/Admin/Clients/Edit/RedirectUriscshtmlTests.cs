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
    private static readonly string ExistingUri = TestValues.NewCallbackUrl();

    private static readonly string PostedUri = TestValues.NewCallbackUrl();

    private static readonly string ReplacedUri = TestValues.NewCallbackUrl();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = ExistingUri, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.RedirectUris);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewUri_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = 0, RedirectUri = PostedUri }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyRedirectUri = Assert.Single(client.RedirectUris);
        Assert.Equal(PostedUri, onlyRedirectUri.RedirectUri);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(RedirectUrisModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesUri_WhenNotPosted()
    {
        // Arrange
        var existing = new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = ReplacedUri, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.RedirectUris);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingRedirectUri_WhenPostedWithId()
    {
        // Arrange
        var existing = new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = ReplacedUri, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), RedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new RedirectUrisModel(ctx.Object)
        {
            RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = PostedUri }],
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(PostedUri, existing.RedirectUri);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.RedirectUris);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };

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
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [new ClientRedirectUri { Id = ExistingEntityId, RedirectUri = ExistingUri }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.RedirectUris);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new RedirectUrisModel(ctx.Object) { RedirectUris = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}