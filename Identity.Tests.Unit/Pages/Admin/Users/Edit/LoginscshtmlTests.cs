namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginscshtmlTests
{
    private static readonly string LoginProvider = TestValues.NewSchemeName();

    private static readonly string LoginProviderKey = TestValues.NewProviderKey();

    private static readonly string LoginDisplayName = TestValues.NewDisplayName();

    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetLoginsAsync(user)).ReturnsAsync([new UserLoginInfo(LoginProvider, LoginProviderKey, LoginDisplayName)]);
        var model = new LoginsModel(um.Object);

        // Act
        var result = await model.OnGetAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        var onlyLogin = Assert.Single(model.Logins);
        Assert.Equal(LoginProvider, onlyLogin.LoginProvider);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginsModel(um.Object);

        // Act
        var result = await model.OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_RemovesAndRedirects_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.RemoveLoginAsync(user, LoginProvider, LoginProviderKey)).ReturnsAsync(IdentityResult.Success);
        var model = new LoginsModel(um.Object);

        // Act
        var result = await model.OnPostRemoveAsync(ExistingUserId, LoginProvider, LoginProviderKey);

        // Assert
        um.Verify(m => m.RemoveLoginAsync(user, LoginProvider, LoginProviderKey), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new LoginsModel(um.Object);

        // Act
        var result = await model.OnPostRemoveAsync(MissingUserId, LoginProvider, LoginProviderKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}