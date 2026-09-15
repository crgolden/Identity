namespace Identity.Tests.Unit.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.Clients;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class CreatecshtmlTests
{
    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new CreateModel(ctx.Object);

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelInvalid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new CreateModel(ctx.Object);
        model.ModelState.AddModelError(nameof(Client.ClientId), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsAndRedirects_WhenValid()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<Client>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.Clients).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new CreateModel(ctx.Object) { Client = new Client { ClientId = TestValues.NewClientIdentifier() } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetailsIndex, redirect.PageName);
    }
}