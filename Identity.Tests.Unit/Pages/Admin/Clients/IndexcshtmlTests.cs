namespace Identity.Tests.Unit.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();

        // Act
        var model = new IndexModel(ctx.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsClientsOrderedByClientId()
    {
        // Arrange
        var firstAlphabetically = TestValues.NewFirstAlphabeticalName();
        var lastAlphabetically = TestValues.NewLastAlphabeticalName();
        var clients = new[]
        {
            new Client { Id = TestValues.NewEntityId(), ClientId = lastAlphabetically },
            new Client { Id = TestValues.NewEntityId(), ClientId = firstAlphabetically },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(clients);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(clients.Length, model.Clients.Count);
        Assert.Equal(firstAlphabetically, model.Clients[0].ClientId);
        Assert.Equal(lastAlphabetically, model.Clients[1].ClientId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsEmpty_WhenNoClients()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Empty(model.Clients);
    }
}