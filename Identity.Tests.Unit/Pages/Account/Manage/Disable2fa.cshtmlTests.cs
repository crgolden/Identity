namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Infrastructure;
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
        var expectedId = TestValues.NewUserId().ToString();
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
        Assert.Equal(UserMessages.UnableToLoadUser(expectedId), message);
    }

    [Fact]
    public async Task OnGet_TwoFactorEnabled_ReturnsPageResult()
    {
        // Arrange
        var model = CreateModelWithTwoFactorState(true);

        // Act
        var result = await model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGet_TwoFactorNotEnabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateModelWithTwoFactorState(false);

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGet());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(Disable2faModel.TwoFactorNotEnabledMessage, ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = TestValues.NewUserId().ToString();
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
        Assert.Equal(UserMessages.UnableToLoadUser(userId), notFound.Value);
        Assert.Null(model.StatusMessage);
    }

    [Fact]
    public async Task OnPostAsync_DisableFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = TestValues.NewUserId() };
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock.Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = TestValues.NewFailureReason() }));

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
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(Disable2faModel.DisableFailedMessage, ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_Succeeds_RedirectsAndSetsStatusMessage()
    {
        // Arrange
        var userId = TestValues.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = TestValues.NewUserId() };
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
        Assert.Equal(PageRoutes.SiblingTwoFactorAuthentication, redirect.PageName);
        Assert.Equal(Disable2faModel.TwoFactorDisabledMessage, model.StatusMessage);
    }

    private static Disable2faModel CreateModelWithTwoFactorState(bool twoFactorEnabled)
    {
        var userManagerMock = MockHelpers.MockUserManager();

        var existingUser = new IdentityUser<Guid>();

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(existingUser);

        userManagerMock
            .Setup(m => m.GetTwoFactorEnabledAsync(existingUser))
            .ReturnsAsync(twoFactorEnabled);

        return new Disable2faModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }
}