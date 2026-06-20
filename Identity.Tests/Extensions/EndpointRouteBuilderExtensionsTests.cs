namespace Identity.Tests.Extensions;
using Infrastructure;

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EndpointRouteBuilderExtensionsTests
{
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
            .Returns("unknown-id");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyCreationOptions", null, TestContext.Current.CancellationToken);

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
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "testuser" };
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(u => u.GetUserIdAsync(user))
            .ReturnsAsync(user.Id.ToString());
        userManagerMock
            .Setup(u => u.GetUserNameAsync(user))
            .ReturnsAsync("testuser");
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .ReturnsAsync("{\"type\":\"webauthn.create\"}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyCreationOptions", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal("{\"type\":\"webauthn.create\"}", body);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyCreationOptions_UserFound_PassesUserEntityToSignInManager()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var userId = Guid.NewGuid();
        var user = new IdentityUser<Guid> { Id = userId, UserName = "alice" };
        PasskeyUserEntity? capturedEntity = null;

        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync(userId.ToString());
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync("alice");
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .Callback<PasskeyUserEntity>(e => capturedEntity = e)
            .ReturnsAsync("{}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            await client.PostAsync("/Account/PasskeyCreationOptions", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(userId.ToString(), capturedEntity.Id);
            Assert.Equal("alice", capturedEntity.Name);
            Assert.Equal("alice", capturedEntity.DisplayName);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        }
    }

    [Fact]
    public async Task PasskeyCreationOptions_NullUserName_UsesUserAsFallbackDisplayName()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var userId = Guid.NewGuid();
        var user = new IdentityUser<Guid> { Id = userId, UserName = null };
        PasskeyUserEntity? capturedEntity = null;

        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(u => u.GetUserIdAsync(user)).ReturnsAsync(userId.ToString());
        userManagerMock.Setup(u => u.GetUserNameAsync(user)).ReturnsAsync((string?)null);
        signInManagerMock
            .Setup(s => s.MakePasskeyCreationOptionsAsync(It.IsAny<PasskeyUserEntity>()))
            .Callback<PasskeyUserEntity>(e => capturedEntity = e)
            .ReturnsAsync("{}");

        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            await client.PostAsync("/Account/PasskeyCreationOptions", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal("User", capturedEntity.Name);
            Assert.Equal("User", capturedEntity.DisplayName);
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
            .ReturnsAsync("{\"type\":\"webauthn.get\"}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyRequestOptions", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(null), Times.Once);
            userManagerMock.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_WhitespaceUsername_MakesRequestOptionsWithNullUser()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(null))
            .ReturnsAsync("{}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyRequestOptions?username=   ", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(null), Times.Once);
            userManagerMock.Verify(u => u.FindByNameAsync(It.IsAny<string>()), Times.Never);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_UsernameProvided_FindsUserAndMakesRequestOptions()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        var user = new IdentityUser<Guid> { UserName = "alice" };
        userManagerMock
            .Setup(u => u.FindByNameAsync("alice"))
            .ReturnsAsync(user);
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(user))
            .ReturnsAsync("{\"type\":\"webauthn.get\"}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyRequestOptions?username=alice", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            signInManagerMock.Verify(s => s.MakePasskeyRequestOptionsAsync(user), Times.Once);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        }
    }

    [Fact]
    public async Task PasskeyRequestOptions_ReturnsOkWithJson()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, antiforgeryMock) = CreateMocks();
        signInManagerMock
            .Setup(s => s.MakePasskeyRequestOptionsAsync(null))
            .ReturnsAsync("{\"challenge\":\"abc\"}");
        var (app, client) = await BuildTestAppAsync(userManagerMock, signInManagerMock, antiforgeryMock);
        await using (app)
        {
            // Act
            var response = await client.PostAsync("/Account/PasskeyRequestOptions", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal("{\"challenge\":\"abc\"}", body);
            antiforgeryMock.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        }
    }

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