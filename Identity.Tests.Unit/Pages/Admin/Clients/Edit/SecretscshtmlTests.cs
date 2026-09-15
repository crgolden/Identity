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
public class SecretscshtmlTests
{
    private static readonly string ExistingSecretValue = TestValues.NewSecretValue();

    private static readonly string PostedSecretValue = TestValues.NewSecretValue();

    private static readonly string ExistingDescription = TestValues.NewDescription();

    private static readonly string PostedDescription = TestValues.NewDescription();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), ClientSecrets = [new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Secrets);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewSecret_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), ClientSecrets = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new SecretsModel(ctx.Object)
        {
            Secrets = [new ClientSecret { Id = 0, Value = PostedSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyClientSecret = Assert.Single(client.ClientSecrets);
        Assert.Equal(PostedSecretValue, onlyClientSecret.Value);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(SecretsModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object) { Secrets = [] };

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
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new SecretsModel(ctx.Object)
        {
            Secrets = [new ClientSecret { Id = ExistingEntityId, Type = IdentityServerConstants.SecretTypes.SharedSecret, Description = PostedDescription }],
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
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), ClientSecrets = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new SecretsModel(ctx.Object) { Secrets = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.ClientSecrets);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRowWithDefaultType_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object) { Secrets = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        var onlySecret = Assert.Single(model.Secrets);
        Assert.Equal(IdentityServerConstants.SecretTypes.SharedSecret, onlySecret.Type);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object) { Secrets = [] };

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
        var model = new SecretsModel(ctx.Object) { Secrets = [new ClientSecret { Id = ExistingEntityId, Value = ExistingSecretValue, Type = IdentityServerConstants.SecretTypes.SharedSecret }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Secrets);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new SecretsModel(ctx.Object) { Secrets = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}