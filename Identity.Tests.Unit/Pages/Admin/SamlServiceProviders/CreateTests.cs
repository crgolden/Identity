namespace Identity.Tests.Unit.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class CreateTests
{
    private static readonly string EntityId = Generated.NewNamespacedEntityId();

    [Fact]
    public void OnGet_ReturnsPage()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new Create(ctx.Object);

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders.Add(It.IsAny<SamlServiceProvider>()));
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Create(ctx.Object) { SamlServiceProvider = new SamlServiceProvider { EntityId = EntityId } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetails, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenInvalid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new Create(ctx.Object);
        model.ModelState.AddModelError(nameof(SamlServiceProvider.EntityId), Generated.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }
}
