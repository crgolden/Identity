namespace Identity.Tests.Unit.Pages.Admin.Roles.Details;

using Identity.Pages.Admin.Roles.Details;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
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
        var model = new IndexModel(rm.Object);

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
        var model = new IndexModel(rm.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}