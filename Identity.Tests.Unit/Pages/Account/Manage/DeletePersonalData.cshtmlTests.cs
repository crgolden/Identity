namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class DeletePersonalDataModelTests
{
    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);

        // Act
        var model = new DeletePersonalDataModel(userManager.Object, signInManager.Object);

        // Assert
        Assert.NotNull(model.Input);
        Assert.False(model.RequirePassword);
    }

    [Fact]
    public async Task OnGet_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGet_UserFound_SetsRequirePasswordAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);

        // Act
        var result = await model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.RequirePassword);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-id");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-id", notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_NoPasswordRequired_DeletesSignsOutAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PasswordRequired_BlankPassword_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        model.Input = new DeletePersonalDataModel.InputModel { Password = null };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        userManager.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PasswordRequired_WrongPassword_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        model.Input = new DeletePersonalDataModel.InputModel { Password = "wrong" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        userManager.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PasswordRequired_CorrectPassword_DeletesSignsOutAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);
        model.Input = new DeletePersonalDataModel.InputModel { Password = "correct" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_DeleteFails_Throws()
    {
        // Arrange
        var (userManager, _, model) = CreateModel();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync());
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> UserManager, Mock<SignInManager<IdentityUser<Guid>>> SignInManager, DeletePersonalDataModel Model) CreateModel()
    {
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        var model = new DeletePersonalDataModel(userManager.Object, signInManager.Object)
        {
            PageContext = MockHelpers.PageContext(),
        };
        return (userManager, signInManager, model);
    }
}
