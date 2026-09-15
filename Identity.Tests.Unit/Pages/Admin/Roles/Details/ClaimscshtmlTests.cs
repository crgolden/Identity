namespace Identity.Tests.Unit.Pages.Admin.Roles.Details;

using System.Security.Claims;
using Identity.Pages.Admin.Roles.Details;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    private static readonly string RoleName = TestValues.NewRoleName();

    private static readonly string ClaimType = TestValues.NewClaimType();

    private static readonly string ClaimValue = TestValues.NewClaimValue();

    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([new Claim(ClaimType, ClaimValue)]);
        var model = new ClaimsModel(rm.Object);

        // Act
        var result = await model.OnGetAsync(role.Id.ToString());

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);
        var model = new ClaimsModel(rm.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}