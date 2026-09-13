namespace Identity.Tests.Unit.Pages.Admin.Roles;

using Identity.Pages.Admin.Roles;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class CreatecshtmlTests
{
    private static readonly string RoleName = TestValues.NewRoleName();

    [Fact]
    public void OnGet_ReturnsPage()
    {
        Assert.IsType<PageResult>(new CreateModel(MockHelpers.MockRoleManager().Object).OnGet());
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>())).ReturnsAsync(IdentityResult.Success);

        var model = new CreateModel(rm.Object) { RoleName = RoleName };
        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetailsIndex, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenInvalid()
    {
        var model = new CreateModel(MockHelpers.MockRoleManager().Object);
        model.ModelState.AddModelError(nameof(CreateModel.RoleName), TestValues.NewValidationMessage());

        Assert.IsType<PageResult>(await model.OnPostAsync());
    }
}