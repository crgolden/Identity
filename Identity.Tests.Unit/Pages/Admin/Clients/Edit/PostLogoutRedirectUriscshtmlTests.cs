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
public class PostLogoutRedirectUriscshtmlTests
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
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = ExistingEntityId, PostLogoutRedirectUri = ExistingUri, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewUri_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), PostLogoutRedirectUris = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PostLogoutRedirectUrisModel(ctx.Object)
        {
            PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = 0, PostLogoutRedirectUri = PostedUri }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyPostLogoutRedirectUri = Assert.Single(client.PostLogoutRedirectUris);
        Assert.Equal(PostedUri, onlyPostLogoutRedirectUri.PostLogoutRedirectUri);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PostLogoutRedirectUrisModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesUri_WhenNotPosted()
    {
        // Arrange
        var existing = new ClientPostLogoutRedirectUri { Id = ExistingEntityId, PostLogoutRedirectUri = ReplacedUri, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), PostLogoutRedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingPostLogoutRedirectUri_WhenPostedWithId()
    {
        // Arrange
        var existing = new ClientPostLogoutRedirectUri { Id = ExistingEntityId, PostLogoutRedirectUri = ReplacedUri, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), PostLogoutRedirectUris = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PostLogoutRedirectUrisModel(ctx.Object)
        {
            PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = ExistingEntityId, PostLogoutRedirectUri = PostedUri }],
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(PostedUri, existing.PostLogoutRedirectUri);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };

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
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [new ClientPostLogoutRedirectUri { Id = ExistingEntityId, PostLogoutRedirectUri = ExistingUri }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.PostLogoutRedirectUris);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PostLogoutRedirectUrisModel(ctx.Object) { PostLogoutRedirectUris = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}