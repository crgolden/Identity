namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

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
public class GenerateRecoveryCodesModelTests
{
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
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
            new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object);

        // Act
        var model = new GenerateRecoveryCodesModel(userManagerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var expectedUserId = "missing-user-id";
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedUserId}'.";
        Assert.Equal(expectedMessage, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expectedMessage, (string)notFound.Value);
    }

    [Fact]
    public async Task OnPostAsync_TwoFactorDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(false);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync("some-user-id");

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await model.OnPostAsync());
        Assert.Equal("Cannot generate recovery codes for user as they do not have 2FA enabled.", ex.Message);
    }

    [Fact]
    public async Task OnPostAsync_TwoFactorEnabled_GeneratesCodesAndRedirects()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(true);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync("active-user-id");

        var generatedCodes = new List<string> { "code1", "code2", "code3" };
        userManagerMock
            .Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync(generatedCodes);

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ShowRecoveryCodes", redirect.PageName);

        Assert.NotNull(model.RecoveryCodes);
        Assert.Equal(generatedCodes.Count, model.RecoveryCodes.Length);
        Assert.Equal(generatedCodes, model.RecoveryCodes.ToList());

        Assert.Equal("You have generated new recovery codes.", model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        // Simulate no user found
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // Ensure GetUserId returns a specific id used in the NotFound message
        const string expectedId = "expected-user-id";
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object);

        // Provide a ClaimsPrincipal for completeness
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
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetAsync_TwoFactorEnabledFlag_BehavesAsExpected(bool isTwoFactorEnabled)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var user = new IdentityUser<Guid>();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(isTwoFactorEnabled);

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object);

        // Provide a simple ClaimsPrincipal (not relied upon by mocked methods, but realistic)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        if (isTwoFactorEnabled)
        {
            var result = await model.OnGetAsync();
            Assert.IsType<PageResult>(result);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync());
            Assert.Equal("Cannot generate recovery codes for user because they do not have 2FA enabled.", ex.Message);
        }
    }
}
