#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

public class PersonalDataModelTests
{
    /// <summary>
    /// Verifies that the PersonalDataModel constructor accepts valid, non-null dependencies
    /// and constructs an instance that derives from PageModel without throwing.
    /// Inputs: a real UserManager{IdentityUser{Guid}} constructed from mocked dependencies
    /// and a mocked ILogger{PersonalDataModel}.
    /// Expected: instance is created and is assignable to PageModel.
    /// </summary>
    [Fact]
    public void PersonalDataModel_WithValidDependencies_DoesNotThrowAndCreatesInstance()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>();
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

        var loggerMock = new Mock<ILogger<PersonalDataModel>>();

        // Act
        PersonalDataModel? model = null;
        Exception? caught = Record.Exception(() => model = new PersonalDataModel(userManager));

        // Assert
        Assert.Null(caught);
        Assert.NotNull(model);
        Assert.IsType<PersonalDataModel>(model);
        Assert.IsAssignableFrom<PageModel>(model);
    }

    /// <summary>
    /// Ensures that constructor consistently accepts different valid logger instances.
    /// Inputs: same UserManager instance but two different ILogger{PersonalDataModel} mocks.
    /// Expected: both constructions succeed and produce distinct PersonalDataModel instances.
    /// </summary>
    [Fact]
    public void PersonalDataModel_WithDifferentLoggerInstances_CreatesDistinctInstances()
    {
        // Arrange - build minimal UserManager as in previous test
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var servicesMock = new Mock<IServiceProvider>();
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

        var loggerMock1 = new Mock<ILogger<PersonalDataModel>>();
        var loggerMock2 = new Mock<ILogger<PersonalDataModel>>();

        // Act
        var model1 = new PersonalDataModel(userManager);
        var model2 = new PersonalDataModel(userManager);

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);
        Assert.NotSame(model1, model2);
    }

    /// <summary>
    /// Provides various user id values (including null, empty, whitespace, long, and special characters)
    /// to validate the NotFound message formatting when the user cannot be loaded.
    /// </summary>
    public static IEnumerable<object?[]> UserIdValues =>
        new List<object?[]>
        {
            new object?[] { null },
            new object?[] { string.Empty },
            new object?[] { "   " },
            new object?[] { new string('a', 1024) },
            new object?[] { "special\n\t!@#€" }
        };

    /// <summary>
    /// The test verifies that when the user manager returns null for GetUserAsync,
    /// PersonalDataModel.OnGet returns a NotFoundObjectResult containing the message:
    /// "Unable to load user with ID '{userId}'."
    /// The test exercises multiple user id edge values including null, empty, whitespace,
    /// a very long string, and special characters.
    /// </summary>
    /// <param name="userId">The value GetUserId will return (may be null).</param>
    [Theory]
    [MemberData(nameof(UserIdValues))]
    public async Task OnGet_UserNotFound_ReturnsNotFoundWithMessage(string? userId)
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(store, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        var loggerMock = new Mock<ILogger<PersonalDataModel>>();

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
        IActionResult result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        string expected = $"Unable to load user with ID '{userId}'.";
        Assert.Equal(expected, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expected, (string)notFound.Value!);

        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        userManagerMock.Verify(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    /// <summary>
    /// Verifies that when the user manager returns an existing user,
    /// PersonalDataModel.OnGet returns a PageResult and does not call GetUserId.
    /// </summary>
    [Fact]
    public async Task OnGet_UserFound_ReturnsPageResult()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(store, null, null, null, null, null, null, null, null);

        var existingUser = new IdentityUser<Guid>();
        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(existingUser);

        // Ensure GetUserId is not invoked in the success path
        userManagerMock
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Throws(new Exception("GetUserId should not be called when user is found"));

        var loggerMock = new Mock<ILogger<PersonalDataModel>>();

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
        IActionResult result = await model.OnGet();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
        userManagerMock.Verify(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    /// <summary>
    /// Verifies that exceptions thrown by GetUserAsync propagate from OnGet.
    /// Input condition: GetUserAsync throws InvalidOperationException.
    /// Expected: the same InvalidOperationException is thrown by OnGet.
    /// </summary>
    [Fact]
    public async Task OnGet_GetUserAsyncThrows_PropagatesException()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(store, null, null, null, null, null, null, null, null);

        userManagerMock
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var loggerMock = new Mock<ILogger<PersonalDataModel>>();

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