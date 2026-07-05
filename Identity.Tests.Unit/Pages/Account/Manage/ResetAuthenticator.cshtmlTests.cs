namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
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
            // Case: user exists -> expect PageResult
            { true, null, typeof(PageResult), null },

            // Case: user not found -> expect NotFound with ID embedded in message
            { false, missingUserId, typeof(NotFoundObjectResult), expectedMessage },
        };
    }

    [Theory]
    [MemberData(nameof(OnGetTestCases))]
    public async Task OnGet_UserExistence_ReturnsExpectedResult(bool userExists, string? expectedUserId, Type expectedResultType, string? expectedMessage)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        // Setup GetUserAsync to return a user or null based on input
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userExists ? new IdentityUser<Guid>() : null);

        // Setup GetUserId to return the provided expectedUserId (may be null)
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManager = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new ResetAuthenticatorModel(userManagerMock.Object, signInManager.Object);

        // Set up a minimal PageContext with a ClaimsPrincipal so PageModel.User is available
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

            // The implementation constructs the message using _userManager.GetUserId(User)
            Assert.Equal(expectedMessage, notFound.Value as string);
        }
        else if (expectedResultType == typeof(PageResult))
        {
            // Nothing else to assert for PageResult beyond type
            Assert.IsType<PageResult>(result);
        }
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage()
    {
        // Arrange
        var userIdString = "missing-user-id";
        var mockUserManager = MockHelpers.MockUserManager();

        // GetUserAsync returns null to simulate missing user
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // GetUserId returns a string used in the NotFound message
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userIdString);

        var mockSignInManager = MockHelpers.MockSignInManager(mockUserManager.Object);

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);

        // Provide a ClaimsPrincipal (not used beyond forwarding to mocks)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userIdString}'.", notFound.Value);

        // Ensure no attempt to modify user state occurred
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

        // Verify operations were attempted regardless of identity result success/failure
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(user, false), Times.Once);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(user), Times.Once);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
    }
}
