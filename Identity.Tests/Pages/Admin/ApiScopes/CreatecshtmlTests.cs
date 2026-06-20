namespace Identity.Tests.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes;
using Identity.Tests.Infrastructure;
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
        var ctx = new Mock<IConfigurationDbContext>();
        Assert.IsType<PageResult>(new CreateModel(ctx.Object).OnGet());
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes.Add(It.IsAny<ApiScope>()));
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new CreateModel(ctx.Object) { Scope = new ApiScope { Name = "api1" } };
        var result = await model.OnPostAsync();
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenInvalid()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new CreateModel(ctx.Object);
        model.ModelState.AddModelError("Scope.Name", "Required");
        Assert.IsType<PageResult>(await model.OnPostAsync());
    }
}
