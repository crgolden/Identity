namespace Identity.Tests.Unit.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiResources;
using Identity.Tests.Unit.Infrastructure;
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
        ctx.Setup(c => c.ApiResources.Add(It.IsAny<ApiResource>()));
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new CreateModel(ctx.Object) { Resource = new ApiResource { Name = "my-api" } };

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenInvalid()
    {
        var ctx = new Mock<IConfigurationDbContext>();
        var model = new CreateModel(ctx.Object);
        model.ModelState.AddModelError("Resource.Name", "Required");

        Assert.IsType<PageResult>(await model.OnPostAsync());
    }
}
