namespace Identity.Tests.Unit.Pages.Admin.Roles;

using Identity.Pages.Admin.Roles;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeletecshtmlTests
{
    private static readonly string RoleName = TestValues.NewRoleName();

    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        var model = new DeleteModel(rm.Object);

        // Act
        var result = await model.OnGetAsync(role.Id.ToString());

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(RoleName, model.AppRole.Name);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);
        var model = new DeleteModel(rm.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_Deletes_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);
        var model = new DeleteModel(rm.Object);

        // Act
        var result = await model.OnPostAsync(role.Id.ToString());

        // Assert
        rm.Verify(m => m.DeleteAsync(role), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingIndex, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);
        var model = new DeleteModel(rm.Object);

        // Act
        var result = await model.OnPostAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}