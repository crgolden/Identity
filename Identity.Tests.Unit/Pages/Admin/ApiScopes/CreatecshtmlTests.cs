namespace Identity.Tests.Unit.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes;
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
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes.Add(It.IsAny<ApiScope>()));
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new CreateModel(ctx.Object) { Scope = new ApiScope { Name = TestValues.NewApiResourceName() } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetailsIndex, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenInvalid()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new CreateModel(ctx.Object);
        model.ModelState.AddModelError(nameof(ApiScope.Name), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }
}