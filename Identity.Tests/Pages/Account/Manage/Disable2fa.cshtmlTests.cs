#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;
using Identity.Tests.Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class Disable2faModelTests
{
    /// <summary>
    /// Verifies that when the user cannot be loaded (UserManager.GetUserAsync returns null),
    /// OnGet returns a NotFoundObjectResult whose message includes the UserManager.GetUserId(User) value.
    /// Input: User principal present, GetUserAsync -> null, GetUserId -> predefined id.
    /// Expected: NotFoundObjectResult with the expected formatted message.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnGet_UserIsNull_ReturnsNotFoundWithUserIdInMessage()
    {
        // Arrange
        var expectedId = "expected-user-id";
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var loggerMock = new Mock<ILogger<Disable2faModel>>();

        var model = new Disable2faModel(userManagerMock.Object, loggerMock.Object)
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

    /// <summary>
    /// Tests OnGet behavior for users that exist with different two-factor enabled states.
    /// Inputs:
    ///  - twoFactorEnabled = true  -> expect PageResult.
    ///  - twoFactorEnabled = false -> expect InvalidOperationException.
    /// This parameterized test covers both the happy path and the invalid state.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGet_TwoFactorState_BehavesAsExpected(bool twoFactorEnabled)
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var existingUser = new IdentityUser<Guid>();

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(existingUser);

        userManagerMock
            .Setup(m => m.GetTwoFactorEnabledAsync(existingUser))
            .ReturnsAsync(twoFactorEnabled);

        var loggerMock = new Mock<ILogger<Disable2faModel>>();

        var model = new Disable2faModel(userManagerMock.Object, loggerMock.Object)
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

    /// <summary>
    /// Verifies that when the user manager cannot find a user for the current principal,
    /// OnPostAsync returns a NotFoundObjectResult with the expected message that includes the user id returned by UserManager.GetUserId.
    /// Input: GetUserAsync returns null, GetUserId returns a non-null id string.
    /// Expected: NotFoundObjectResult with message containing the id.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = "missing-user-id";
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var loggerMock = new Mock<ILogger<Disable2faModel>>();
        var model = new Disable2faModel(userManagerMock.Object, loggerMock.Object)
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

    /// <summary>
    /// Verifies that when disabling 2FA fails (IdentityResult.Failed), OnPostAsync throws InvalidOperationException.
    /// Input: GetUserAsync returns a valid user, SetTwoFactorEnabledAsync returns a failed IdentityResult.
    /// Expected: InvalidOperationException with the defined message.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAsync_DisableFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "some-error" }));

        var loggerMock = new Mock<ILogger<Disable2faModel>>();
        var model = new Disable2faModel(userManagerMock.Object, loggerMock.Object)
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

    /// <summary>
    /// Verifies that when disabling 2FA succeeds, OnPostAsync logs the event, sets StatusMessage, and redirects to TwoFactorAuthentication page.
    /// Input: GetUserAsync returns a valid user, SetTwoFactorEnabledAsync returns IdentityResult.Success, GetUserId returns a user id string.
    /// Expected: RedirectToPageResult to ./TwoFactorAuthentication, StatusMessage set to expected string, logger invoked with information level.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAsync_Succeeds_RedirectsAndSetsStatusMessageAndLogs()
    {
        // Arrange
        var userId = "user-123";
        var userStoreMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock, null, null, null, null, null, null, null, null);

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var loggerMock = new Mock<ILogger<Disable2faModel>>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(true);
        var model = new Disable2faModel(userManagerMock.Object, loggerMock.Object)
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

        // Verify logger was called with an information level entry containing the expected phrase.
#pragma warning disable CA1873
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() != null && $"{v}".Contains("has disabled 2fa.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
#pragma warning restore CA1873
    }
}