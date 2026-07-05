namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class Disable2faModelTests
{
    [Fact]
    public async Task OnGet_UserIsNull_ReturnsNotFoundWithUserIdInMessage()
    {
        // Arrange
        var expectedId = "expected-user-id";
        var userManagerMock = MockHelpers.MockUserManager();

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGet_TwoFactorState_BehavesAsExpected(bool twoFactorEnabled)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var existingUser = new IdentityUser<Guid>();

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(existingUser);

        userManagerMock
            .Setup(m => m.GetTwoFactorEnabledAsync(existingUser))
            .ReturnsAsync(twoFactorEnabled);

        var model = new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act & Assert
        if (twoFactorEnabled)
        {
            var result = await model.OnGet();
            Assert.IsType<PageResult>(result);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGet());
            Assert.Equal("Cannot disable 2FA for user as it's not currently enabled.", ex.Message);
        }
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = "missing-user-id";
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var model = new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userId}'.", notFound.Value);
        Assert.Null(model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_DisableFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "some-error" }));

        var model = new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnPostAsync());
        Assert.Equal("Unexpected error occurred disabling 2FA.", ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_Succeeds_RedirectsAndSetsStatusMessage()
    {
        // Arrange
        var userId = "user-123";
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var model = new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./TwoFactorAuthentication", redirect.PageName);
        Assert.Equal("2fa has been disabled. You can reenable 2fa when you setup an authenticator app", model.StatusMessage);
    }
}
