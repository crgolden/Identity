namespace Identity.Tests.Unit.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class CreatecshtmlTests
{
    private static readonly string SchemeName = TestValues.NewSchemeName();

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
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders.Add(It.IsAny<IdentityProvider>()));
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new CreateModel(ctx.Object) { IdentityProvider = new IdentityProvider { Scheme = SchemeName } };

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
        var model = new CreateModel(ctx.Object);
        model.ModelState.AddModelError(nameof(IdentityProvider.Scheme), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }
}