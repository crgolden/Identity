namespace Identity.Tests.Pages.Account.Manage;
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
public class DeletePersonalDataModelTests
{
    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        // Create required mocks and helper objects for UserManager dependencies.
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>(MockBehavior.Strict);
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>(MockBehavior.Strict);
        var identityErrorDescriber = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>(MockBehavior.Loose);
        var userManagerLoggerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();

        // Create a concrete UserManager instance using mocked dependencies.
        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            optionsMock.Object,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            lookupNormalizerMock.Object,
            identityErrorDescriber,
            serviceProviderMock.Object,
            userManagerLoggerMock.Object);

        // Arrange SignInManager dependencies.
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        var userClaimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var signInManagerLoggerMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict);
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManager,
            httpContextAccessorMock.Object,
            userClaimsFactoryMock.Object,
            optionsMock.Object,
            signInManagerLoggerMock.Object,
            schemesMock.Object,
            userConfirmationMock.Object);

        // Act
        var model = new DeletePersonalDataModel(userManager, signInManager);

        // Assert
        Assert.NotNull(model);
        Assert.NotNull(model.Input);
        Assert.False(model.RequirePassword);
    }

    [Fact]
    public async Task OnGet_UserNotFound_ReturnsNotFoundObjectResultWithMessage()
    {
        // Arrange
        var expectedUserId = "missing-user-123";
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            new List<IUserValidator<IdentityUser<Guid>>>(),
            new List<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var model = new DeletePersonalDataModel(mockUserManager.Object, signInManagerMock.Object);

        // Ensure PageModel.User is populated (the actual claims are not used by the mock setup that uses It.IsAny)
        var principal = new ClaimsPrincipal(new ClaimsIdentity([]));
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await model.OnGet();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedUserId}'.";
        Assert.Equal(expectedMessage, notFound.Value);
    }
}
