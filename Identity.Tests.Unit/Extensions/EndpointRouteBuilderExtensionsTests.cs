namespace Identity.Tests.Unit.Extensions;

using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using Identity.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EndpointRouteBuilderExtensionsTests
{
    private static readonly string SignedInUserName = TestValues.NewUserName();

    [Fact]
    public async Task PasskeyCreationOptions_UserNotFound_Returns404()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(TestValues.NewUserId().ToString());
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(PasskeyEndpoints.CreationOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyCreationOptions_UserFound_ReturnsOkWithJson()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var userId = TestValues.NewUserId();
        var user = new IdentityUser<Guid> { Id = userId, UserName = SignedInUserName };
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(u => u.GetUserIdAsync(user))
            .ReturnsAsync(user.Id.ToString());
        userManagerMock
            .Setup(u => u.GetUserNameAsync(user))
            .ReturnsAsync(SignedInUserName);
        var creationOptionsJson = TestValues.NewPasskeyOptionsJson();
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .ReturnsAsync(creationOptionsJson);
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(PasskeyEndpoints.CreationOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(creationOptionsJson, body);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var userId = TestValues.NewUserId();
        var user = new IdentityUser<Guid> { Id = userId, UserName = SignedInUserName };
        PasskeyUserEntity? capturedEntity = null;

        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync(userId.ToString());
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync(SignedInUserName);
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .Callback<PasskeyUserEntity>(e => capturedEntity = e)
            .ReturnsAsync(TestValues.NewPasskeyOptionsJson());
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            await client.PostAsync(PasskeyEndpoints.CreationOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(userId.ToString(), capturedEntity.Id);
            Assert.Equal(SignedInUserName, capturedEntity.Name);
            Assert.Equal(SignedInUserName, capturedEntity.DisplayName);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyCreationOptions_NullUserName_UsesUserAsFallbackDisplayName()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var userId = TestValues.NewUserId();
        var user = new IdentityUser<Guid> { Id = userId, UserName = null };
        PasskeyUserEntity? capturedEntity = null;

        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync(userId.ToString());
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync((string?)null);
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .Callback<PasskeyUserEntity>(e => capturedEntity = e)
            .ReturnsAsync(TestValues.NewPasskeyOptionsJson());

        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            await client.PostAsync(PasskeyEndpoints.CreationOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(PasskeyEndpoints.FallbackUserName, capturedEntity.Name);
            Assert.Equal(PasskeyEndpoints.FallbackUserName, capturedEntity.DisplayName);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_NullUsername_MakesRequestOptionsWithNullUser()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(null))
            .ReturnsAsync(TestValues.NewPasskeyOptionsJson());
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(PasskeyEndpoints.RequestOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(null), Times.Once);
            userManagerMock.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(null))
            .ReturnsAsync(TestValues.NewPasskeyOptionsJson());
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(UserNameQuery(TestValues.NewWhitespaceValue()), null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(null), Times.Once);
            userManagerMock.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var user = new IdentityUser<Guid> { UserName = SignedInUserName };
        userManagerMock
            .Setup(u => u.FindByNameAsync(SignedInUserName))
            .ReturnsAsync(user);
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(user))
            .ReturnsAsync(TestValues.NewPasskeyOptionsJson());
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(UserNameQuery(SignedInUserName), null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(user), Times.Once);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_ReturnsOkWithJson()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var requestOptionsJson = TestValues.NewPasskeyOptionsJson();
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(null))
            .ReturnsAsync(requestOptionsJson);
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync(PasskeyEndpoints.RequestOptionsPath, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(requestOptionsJson, body);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    private static string UserNameQuery(string userName) =>
        PasskeyEndpoints.RequestOptionsPath + '?' + PasskeyEndpoints.UserNameQueryKey + '=' + userName;

    private static (Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
                    Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock,
                    Mock<IAntiforgery> antiforgeryMock) CreateMocks()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var antiforgeryMock = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgeryMock
            .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        return (userManagerMock, signInManagerMock, antiforgeryMock);
    }

    private static async Task<(WebApplication app, HttpClient client)> BuildTestAppAsync(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock,
        Mock<IAntiforgery> antiforgeryMock)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<UserManager<IdentityUser<Guid>>>(userManagerMock.Object);
        builder.Services.AddSingleton<SignInManager<IdentityUser<Guid>>>(signInManagerMock.Object);
        builder.Services.AddSingleton<IAntiforgery>(antiforgeryMock.Object);

        var app = builder.Build();
        app.UseRouting();
        Identity.Extensions.EndpointRouteBuilderExtensions.MapAdditionalIdentityEndpoints(app);
        await app.StartAsync();
        return (app, app.GetTestClient());
    }
}