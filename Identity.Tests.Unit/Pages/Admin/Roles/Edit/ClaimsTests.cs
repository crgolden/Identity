namespace Identity.Tests.Unit.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Roles.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimsTests
{
    private static readonly string RoleName = Generated.NewRoleName();

    private static readonly string ClaimType = Generated.NewClaimType();

    private static readonly string ClaimValue = Generated.NewClaimValue();

    private static readonly string RemovedClaimType = Generated.NewClaimType();

    private static readonly string RemovedClaimValue = Generated.NewClaimValue();

    private static readonly string PostedClaimType = Generated.NewClaimType();

    private static readonly string PostedClaimValue = Generated.NewClaimValue();

    private static readonly string MissingUserId = Generated.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([new Claim(ClaimType, ClaimValue)]);
        var model = new Claims(rm.Object);

        // Act
        var result = await model.OnGetAsync(role.Id.ToString());

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(RoleName, model.RoleName);
        Assert.Single(model.Resources);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        // Act
        var result = await new Claims(rm.Object).OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var existing = new Claim(RemovedClaimType, RemovedClaimValue);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([existing]);
        rm.Setup(m => m.RemoveClaimAsync(role, existing)).ReturnsAsync(IdentityResult.Success);
        rm.Setup(m => m.AddClaimAsync(role, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
        var model = new Claims(rm.Object) { Resources = [new Claims.ClaimInputModel { Type = PostedClaimType, Value = PostedClaimValue }] };

        // Act
        var result = await model.OnPostAsync(role.Id.ToString());

        // Assert
        rm.Verify(m => m.RemoveClaimAsync(role, existing), Times.Once);
        rm.Verify(m => m.AddClaimAsync(role, It.IsAny<Claim>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Claims.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_RowWasAddedButNeverFilledIn_DropsItAndKeepsTheRest()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([]);
        var saved = new List<Claim>();
        rm.Setup(m => m.AddClaimAsync(role, It.IsAny<Claim>()))
            .Callback<IdentityRole<Guid>, Claim>((_, claim) => saved.Add(claim))
            .ReturnsAsync(IdentityResult.Success);

        var model = new Claims(rm.Object)
        {
            Resources =
            [
                new Claims.ClaimInputModel { Type = PostedClaimType, Value = PostedClaimValue },
                new Claims.ClaimInputModel(),
            ],
        };

        // Act
        await model.OnPostAsync(role.Id.ToString());

        // Assert
        var onlySaved = Assert.Single(saved);
        Assert.Equal(PostedClaimType, onlySaved.Type);
        Assert.Equal(PostedClaimValue, onlySaved.Value);
    }

    [Fact]
    public async Task OnPostAsync_EveryRowIsBlank_SavesNothing()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName);
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        rm.Setup(m => m.GetClaimsAsync(role)).ReturnsAsync([]);
        var model = new Claims(rm.Object) { Resources = [new Claims.ClaimInputModel()] };

        // Act
        var result = await model.OnPostAsync(role.Id.ToString());

        // Assert
        rm.Verify(m => m.AddClaimAsync(role, It.IsAny<Claim>()), Times.Never);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);

        // Act
        var result = await new Claims(rm.Object).OnPostAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        var model = new Claims(rm.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(role.Id.ToString());

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Resources);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);
        var model = new Claims(rm.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var role = new IdentityRole<Guid>(RoleName) { Name = RoleName };
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(role.Id.ToString())).ReturnsAsync(role);
        var model = new Claims(rm.Object) { Resources = [new Claims.ClaimInputModel { Type = ClaimType, Value = ClaimValue }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(role.Id.ToString(), 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        rm.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((IdentityRole<Guid>?)null);
        var model = new Claims(rm.Object) { Resources = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingUserId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
