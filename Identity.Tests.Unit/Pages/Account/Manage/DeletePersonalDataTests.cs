namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class DeletePersonalDataTests
{
    private static readonly string MissingUserId = Generated.NewUserId().ToString();

    private static readonly string CorrectPassword = Generated.NewPassword();

    private static readonly string IncorrectPassword = Generated.NewPassword();

    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);

        // Act
        var model = new DeletePersonalData(userManager.Object, signInManager.Object);

        // Assert
        Assert.NotNull(model.Input);
        Assert.False(model.RequirePassword);
    }

    [Fact]
    public async Task OnGet_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManager, _, model) = Create();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnGet_UserFound_SetsRequirePasswordAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = Create();
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
        var (userManager, _, model) = Create();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(MissingUserId);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(MissingUserId, notFound.Value as string, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostAsync_NoPasswordRequired_DeletesSignsOutAndRedirects()
    {
        // Arrange
        var (userManager, signInManager, model) = Create();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(PageRoutes.ContentRoot, redirect.Url);
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PasswordRequired_BlankPassword_AddsModelErrorAndReturnsPage()
    {
        // Arrange
        var (userManager, _, model) = Create();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        model.Input = new DeletePersonalData.InputModel { Password = null };

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
        var (userManager, _, model) = Create();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        userManager.Setup(m => m.CheckPasswordAsync(user, IncorrectPassword)).ReturnsAsync(false);
        model.Input = new DeletePersonalData.InputModel { Password = IncorrectPassword };

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
        var (userManager, signInManager, model) = Create();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        userManager.Setup(m => m.CheckPasswordAsync(user, CorrectPassword)).ReturnsAsync(true);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);
        model.Input = new DeletePersonalData.InputModel { Password = CorrectPassword };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(PageRoutes.ContentRoot, redirect.Url);
        signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_DeleteFails_Throws()
    {
        // Arrange
        var (userManager, _, model) = Create();
        var user = MockHelpers.TestUser();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() }));

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> UserManager, Mock<SignInManager<IdentityUser<Guid>>> SignInManager, DeletePersonalData Model) Create()
    {
        var userManager = MockHelpers.MockUserManager();
        var signInManager = MockHelpers.MockSignInManager(userManager.Object);
        var model = new DeletePersonalData(userManager.Object, signInManager.Object)
        {
            PageContext = MockHelpers.PageContext(),
        };
        return (userManager, signInManager, model);
    }
}
