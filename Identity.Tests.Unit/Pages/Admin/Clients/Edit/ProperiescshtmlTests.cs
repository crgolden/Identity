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
public class ProperiescshtmlTests
{
    private static readonly string ExistingKey = TestValues.NewPropertyKey();

    private static readonly string ExistingValue = TestValues.NewPropertyValue();

    private static readonly string PostedKey = TestValues.NewPropertyKey();

    private static readonly string PostedValue = TestValues.NewPropertyValue();

    private static readonly string RemovedKey = TestValues.NewPropertyKey();

    private static readonly string RemovedValue = TestValues.NewPropertyValue();

    private static readonly string PriorValue = TestValues.NewPropertyValue();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [new ClientProperty { Id = ExistingEntityId, Key = ExistingKey, Value = ExistingValue, ClientId = ExistingEntityId }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewProperty_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = 0, Key = PostedKey, Value = PostedValue }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyProperty = Assert.Single(client.Properties);
        Assert.Equal(PostedKey, onlyProperty.Key);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PropertiesModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesProperty_WhenNotPosted()
    {
        // Arrange
        var existing = new ClientProperty { Id = ExistingEntityId, Key = RemovedKey, Value = RemovedValue, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(client.Properties);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesExistingProperty_WhenPostedWithId()
    {
        // Arrange
        var existing = new ClientProperty { Id = ExistingEntityId, Key = PostedKey, Value = PriorValue, ClientId = ExistingEntityId };
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ClientProperty { Id = ExistingEntityId, Key = PostedKey, Value = PostedValue }],
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(PostedValue, existing.Value);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

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
        var model = new PropertiesModel(ctx.Object) { Properties = [new ClientProperty { Id = ExistingEntityId, Key = ExistingKey, Value = ExistingValue }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Properties);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}