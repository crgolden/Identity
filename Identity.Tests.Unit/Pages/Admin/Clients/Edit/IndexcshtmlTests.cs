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
public class IndexcshtmlTests
{
    private static readonly string ExistingClientId = TestValues.NewClientIdentifier();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ExistingEntityId, model.Client.Id);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelInvalid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new IndexModel(ctx.Object);
        model.ModelState.AddModelError(nameof(Client.ClientId), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        // Arrange
        var client = new Client { Id = ExistingEntityId, ClientId = TestValues.NewClientIdentifier() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([client]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new IndexModel(ctx.Object) { Client = new Client { ClientId = ExistingClientId } };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(ExistingClientId, client.ClientId);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(IndexModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object) { Client = new Client { ClientId = TestValues.NewClientIdentifier() } };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}