namespace Identity.Tests.Unit.Pages.Admin.Clients.Edit;

using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class SecretsTests
{
    private static readonly string ExistingSecretValue = Generated.NewSecretValue();

    private static readonly string PostedSecretValue = Generated.NewSecretValue();

    private static readonly string ExistingDescription = Generated.NewDescription();

    private static readonly string PostedDescription = Generated.NewDescription();

    private static readonly int ExistingEntityId = Generated.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier(), ClientSecrets = [new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Resources);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewSecret_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier(), ClientSecrets = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Secrets(ctx.Object)
        {
            Resources = [new ClientSecret { Id = 0, Value = PostedSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyClientSecret = Assert.Single(client.ClientSecrets);
        Assert.Equal(PostedSecretValue, onlyClientSecret.Value);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Secrets.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingSecret_WhenPostedWithId()
    {
        // Arrange
        var existing = new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret, Description = ExistingDescription, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier(), ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Secrets(ctx.Object)
        {
            Resources = [new ClientSecret { Id = ExistingEntityId, Type = IdentityServerConstants.SecretTypes.SharedSecret, Description = PostedDescription }],
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(PostedDescription, existing.Description);
        Assert.Equal(ExistingSecretValue, existing.Value);
    }

    [Fact]
    public async Task OnPostAsync_RemovesSecret_WhenNotPosted()
    {
        // Arrange
        var existing = new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier(), ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Secrets(ctx.Object) { Resources = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.ClientSecrets);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRowWithDefaultType_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        var onlySecret = Assert.Single(model.Resources);
        Assert.Equal(IdentityServerConstants.SecretTypes.SharedSecret, onlySecret.Type);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = Generated.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object) { Resources = [new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new Secrets(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
