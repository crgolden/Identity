namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RolesTests
{
    private static readonly string RoleName = Generated.NewRoleName();

    private static readonly string PriorRoleName = Generated.NewRoleName();

    private static readonly string ExistingUserId = Generated.NewUserId().ToString();
    private static readonly string MissingUserId = Generated.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([RoleName]);
        var model = new Roles(um.Object);

        // Act
        var result = await model.OnGetAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        var onlyRole = Assert.Single(model.Resources);
        Assert.Equal(RoleName, onlyRole);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new Roles(um.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesRoles_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([PriorRoleName]);
        um.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        var model = new Roles(um.Object) { Resources = [RoleName] };

        // Act
        var result = await model.OnPostAsync(ExistingUserId);

        // Assert
        um.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        um.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Roles.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_RowWasAddedButNeverFilledIn_DropsItAndKeepsTheRest()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        um.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        var saved = new List<string>();
        um.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .Callback<IdentityUser<Guid>, IEnumerable<string>>((_, roles) => saved.AddRange(roles))
            .ReturnsAsync(IdentityResult.Success);
        var model = new Roles(um.Object) { Resources = [RoleName, null] };

        // Act
        await model.OnPostAsync(ExistingUserId);

        // Assert
        Assert.Equal([RoleName], saved);
    }

    [Fact]
    public async Task OnPostAsync_EveryRowIsBlank_SavesNothing()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([PriorRoleName]);
        um.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        var model = new Roles(um.Object) { Resources = [null] };

        // Act
        var result = await model.OnPostAsync(ExistingUserId);

        // Assert
        um.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Never);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new Roles(um.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        var model = new Roles(um.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Null(Assert.Single(model.Resources));
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new Roles(um.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = Generated.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        var model = new Roles(um.Object) { Resources = [RoleName] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingUserId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new Roles(um.Object) { Resources = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingUserId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
