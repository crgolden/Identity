namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResetAuthenticatorModelTests
{
    public static TheoryData<bool, string?, Type, string?> OnGetTestCases()
    {
        const string missingUserId = "user-123";
        var expectedMessage = $"Unable to load user with ID '{missingUserId}'.";
        return new TheoryData<bool, string?, Type, string?>
        {
            { true, null, typeof(PageResult), null },
            { false, missingUserId, typeof(NotFoundObjectResult), expectedMessage },
        };
    }

    [Theory]
    [MemberData(nameof(OnGetTestCases))]
    public async Task OnGet_UserExistence_ReturnsExpectedResult(bool userExists, string? expectedUserId, Type expectedResultType, string? expectedMessage)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userExists ? new IdentityUser<Guid>() : null);

        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManager = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new ResetAuthenticatorModel(userManagerMock.Object, signInManager.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
            new Claim(ClaimTypes.NameIdentifier, expectedUserId ?? string.Empty)
        ], "test"));

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await model.OnGet();

        // Assert
        Assert.IsType(expectedResultType, result);

        if (expectedResultType == typeof(NotFoundObjectResult))
        {
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(expectedMessage, notFound.Value as string);
        }
        else if (expectedResultType == typeof(PageResult))
        {
            Assert.IsType<PageResult>(result);
        }
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage()
    {
        // Arrange
        var userIdString = "missing-user-id";
        var mockUserManager = MockHelpers.MockUserManager();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userIdString);

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userIdString}'.", notFound.Value);
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>()), Times.Never);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnPostAsync_UserExists_ResetsAndRedirectsRegardlessOfIdentityResult(bool succeedOperations)
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "tester" };

        var mockUserManager = MockHelpers.MockUserManager();

        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var identityResult = succeedOperations ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "fail" });

        mockUserManager
            .Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(identityResult);

        mockUserManager
            .Setup(um => um.ResetAuthenticatorKeyAsync(user))
            .ReturnsAsync(identityResult);

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        mockSignInManager
            .Setup(sm => sm.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./EnableAuthenticator", redirect.PageName);
        Assert.Equal("Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.", model.StatusMessage);
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(user, false), Times.Once);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(user), Times.Once);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
    }
}