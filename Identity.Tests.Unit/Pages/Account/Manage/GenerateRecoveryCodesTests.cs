namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GenerateRecoveryCodesTests
{
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var services = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizerMock.Object,
            new IdentityErrorDescriber(),
            services.Object,
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);

        // Act
        var model = new GenerateRecoveryCodes(userManagerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var expectedUserId = Generated.NewUserId().ToString();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var model = new GenerateRecoveryCodes(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = UserMessages.UnableToLoadUser(expectedUserId);
        Assert.Equal(expectedMessage, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expectedMessage, (string)notFound.Value);
    }

    [Fact]
    public async Task OnPostAsync_TwoFactorDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(false);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync(Generated.NewUserId().ToString());

        var model = new GenerateRecoveryCodes(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostAsync());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(GenerateRecoveryCodes.TwoFactorNotEnabledOnPostMessage, ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_TwoFactorEnabled_GeneratesCodesAndRedirects()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(true);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync(Generated.NewUserId().ToString());

        var generatedCodes = new List<string> { Generated.NewRecoveryCode(), Generated.NewRecoveryCode(), Generated.NewRecoveryCode() };
        userManagerMock
            .Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, GenerateRecoveryCodes.RecoveryCodeCount))
            .ReturnsAsync(generatedCodes);

        var model = new GenerateRecoveryCodes(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingShowRecoveryCodes, redirect.PageName);

        Assert.NotNull(model.RecoveryCodes);
        Assert.Equal(generatedCodes.Count, model.RecoveryCodes.Length);
        Assert.Equal(generatedCodes, model.RecoveryCodes.ToList());

        Assert.Equal(GenerateRecoveryCodes.RecoveryCodesGeneratedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var expectedId = Generated.NewUserId().ToString();
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new GenerateRecoveryCodes(userManagerMock.Object);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, expectedId)
                ]))
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal(UserMessages.UnableToLoadUser(expectedId), message);
    }

    [Fact]
    public async Task OnGetAsync_TwoFactorEnabled_ReturnsPageResult()
    {
        // Arrange
        var model = CreateModelWithTwoFactorState(true);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_TwoFactorNotEnabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateModelWithTwoFactorState(false);

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGetAsync());

        // Assert
        var ex = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(GenerateRecoveryCodes.TwoFactorNotEnabledOnGetMessage, ex.Message);
    }

    private static GenerateRecoveryCodes CreateModelWithTwoFactorState(bool isTwoFactorEnabled)
    {
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid>();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(isTwoFactorEnabled);

        return new GenerateRecoveryCodes(userManagerMock.Object)
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
