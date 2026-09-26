namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PersonalDataTests
{
    public static TheoryData<string?> UserIdValues => new()
    {
        (string?)null,
        string.Empty,
        Generated.NewWhitespaceValue(),
        Generated.NewOverlongValue(),
        Generated.NewControlAndSymbolValue(),
    };

    [Fact]
    public void PersonalDataModel_WithValidDependencies_DoesNotThrowAndCreatesInstance()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLogger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;

        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errorDescriber,
            servicesMock.Object,
            userManagerLogger);

        // Act
        PersonalData? model = null;
        var caught = Record.Exception(() => model = new PersonalData(userManager));

        // Assert
        Assert.Null(caught);
        Assert.NotNull(model);
        Assert.IsType<PersonalData>(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public void PersonalDataModel_WithDifferentLoggerInstances_CreatesDistinctInstances()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLogger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;

        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errorDescriber,
            servicesMock.Object,
            userManagerLogger);

        // Act
        var model1 = new PersonalData(userManager);
        var model2 = new PersonalData(userManager);

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);
        Assert.NotSame(model1, model2);
    }

    [Theory]
    [MemberData(nameof(UserIdValues))]
    public async Task OnGet_UserNotFound_ReturnsNotFoundWithMessage(string? userId)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var model = new PersonalData(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            }
        };

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expected = UserMessages.UnableToLoadUser(userId);
        Assert.Equal(expected, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expected, (string)notFound.Value);

        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        userManagerMock.Verify(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task OnGet_UserFound_ReturnsPageResult()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var existingUser = new IdentityUser<Guid>();
        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(existingUser);

        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Throws(new InvalidOperationException(Generated.NewFailureReason()));

        var model = new PersonalData(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            }
        };

        // Act
        var result = await model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        userManagerMock.Verify(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task OnGet_GetUserAsyncThrows_PropagatesException()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException(Generated.NewFailureReason()));

        var model = new PersonalData(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            }
        };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGet());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }
}
