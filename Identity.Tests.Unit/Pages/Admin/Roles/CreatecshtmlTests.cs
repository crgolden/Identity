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
        // Arrange
        var model = new CreateModel(MockHelpers.MockRoleManager().Object);

        // Act
        var result = model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_Redirects_WhenValid()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>())).ReturnsAsync(IdentityResult.Success);
        var model = new CreateModel(rm.Object) { RoleName = RoleName };

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
        var model = new CreateModel(MockHelpers.MockRoleManager().Object);
        model.ModelState.AddModelError(nameof(CreateModel.RoleName), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RoleNameIsBlank_ReturnsPageWithoutCreatingTheRole()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        var model = new CreateModel(rm.Object);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        rm.Verify(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()), Times.Never);
        Assert.IsType<PageResult>(result);
        Assert.True(model.ModelState.ContainsKey(nameof(CreateModel.RoleName)));
    }
}