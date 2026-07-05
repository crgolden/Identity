namespace Identity.Tests.Unit.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PersonalDataModelTests
{
    public static TheoryData<string?> UserIdValues => new()
    {
        (string?)null,
        string.Empty,
        "   ",
        new string('a', 1024),
        "special\n\t!@#\ufffd",
    };

    [Fact]
    public void PersonalDataModel_WithValidDependencies_DoesNotThrowAndCreatesInstance()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLoggerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();

        // Construct a real UserManager using mocked dependencies (allowed per requirements)
        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errorDescriber,
            servicesMock.Object,
            userManagerLoggerMock.Object);

        // Act
        PersonalDataModel? model = null;
        var caught = Record.Exception(() => model = new PersonalDataModel(userManager));

        // Assert
        Assert.Null(caught);
        Assert.NotNull(model);
        Assert.IsType<PersonalDataModel>(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public void PersonalDataModel_WithDifferentLoggerInstances_CreatesDistinctInstances()
    {
        // Arrange - build minimal UserManager as in previous test
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLoggerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            errorDescriber,
            servicesMock.Object,
            userManagerLoggerMock.Object);

        // Act
        var model1 = new PersonalDataModel(userManager);
        var model2 = new PersonalDataModel(userManager);

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

        var model = new PersonalDataModel(userManagerMock.Object)
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
        var expected = $"Unable to load user with ID '{userId}'.";
        Assert.Equal(expected, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expected, (string)notFound.Value!);

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

        // Ensure GetUserId is not invoked in the success path
        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Throws(new Exception("GetUserId should not be called when user is found"));

        var model = new PersonalDataModel(userManagerMock.Object)
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
            .ThrowsAsync(new InvalidOperationException("boom"));

        var model = new PersonalDataModel(userManagerMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal()
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGet());
        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }
}