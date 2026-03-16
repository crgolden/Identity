#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Linq.Expressions;
using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Trait("Category", "Unit")]
public class RenamePasskeyModelTests
{
    /// <summary>
    /// Tests that when no user is returned from UserManager.GetUserAsync the handler returns NotFound with the expected message.
    /// Condition: UserManager.GetUserAsync returns null and UserManager.GetUserId returns a known id.
    /// Expected: NotFoundObjectResult whose value contains the expected user id message.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>? )null);
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("expected-user-id");
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
        var mockDb = new Mock<ApplicationDbContext>(dbOptions);
        var model = new RenamePasskeyModel(mockUserManager.Object, mockDb.Object);
        // Provide a ClaimsPrincipal so the methods receive a non-null principal
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "expected-user-id")
        ]));
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        // Act
        var result = await model.OnGetAsync("any-id");
        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains("Unable to load user with ID 'expected-user-id'.", message);
    }

    /// <summary>
    /// Tests that when the provided id cannot be decoded as Base64Url the handler sets StatusMessage and redirects to Passkeys page.
    /// Condition: User exists, provided id is invalid Base64Url.
    /// Expected: RedirectToPageResult pointing to ./Passkeys and StatusMessage set to the invalid format message.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_InvalidBase64Id_RedirectsToPasskeysAndSetsStatusMessage()
    {
        // Arrange
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
        var mockDb = new Mock<ApplicationDbContext>(dbOptions);
        var model = new RenamePasskeyModel(mockUserManager.Object, mockDb.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        var invalidId = "!!!invalid-base64url$$$";
        // Act
        var result = await model.OnGetAsync(invalidId);
        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Passkeys", redirect.PageName);
        Assert.Equal("The specified passkey ID had an invalid format.", model.StatusMessage);
    }

    /// <summary>
    /// Tests that when a passkey is not found for a decoded credential id the handler returns NotFound with expected message.
    /// Condition: User exists, id decodes successfully but GetPasskeyAsync returns null.
    /// Expected: NotFoundObjectResult containing the expected passkey-not-found message with user id.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_PasskeyNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        // Ensure GetPasskeyAsync returns null
        mockUserManager.Setup(m => m.GetPasskeyAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<byte[]>())).ReturnsAsync((UserPasskeyInfo? )null);
        // Provide a predictable GetUserId return
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user-42");
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
        var mockDb = new Mock<ApplicationDbContext>(dbOptions);
        var model = new RenamePasskeyModel(mockUserManager.Object, mockDb.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
        // Create a valid Base64Url string for bytes [1,2,3] -> "AQID"
        var validId = "AQID";
        // Act
        var result = await model.OnGetAsync(validId);
        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains("Unable to load passkey ID 'user-42'.", message);
    }

    /// <summary>
    /// Verifies that when the current user cannot be loaded (GetUserAsync returns null),
    /// OnPostAsync returns a NotFoundObjectResult whose message contains the string returned by GetUserId.
    /// Input and DbContext are irrelevant for this path.
    /// Expected: NotFoundObjectResult with message containing returned id.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(store, null, null, null, null, null, null, null, null);
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>? )null);
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("missing-user-id");
        ApplicationDbContext? mockDb = null;
        var model = new RenamePasskeyModel(mockUserManager.Object, mockDb);
        // Provide a principal so PageModel.User is not null
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        // Act
        var result = await model.OnPostAsync();
        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("missing-user-id", Convert.ToString(notFound.Value));
    }

    private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression) : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return default;
        }

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
        public T Current => _inner.Current;
    }

    /// <summary>
    /// Provides valid constructor argument combinations:
    /// - A mocked ApplicationDbContext instance and a constructed UserManager&lt;IdentityUser&lt;Guid&gt; &gt;.
    /// - A real ApplicationDbContext constructed with default DbContextOptions and the same UserManager instance.
    /// This member returns tuples of (ApplicationDbContext, UserManager&lt;IdentityUser&lt;Guid&gt; &gt;).
    /// </summary>
    public static TheoryData<ApplicationDbContext, UserManager<IdentityUser<Guid>>> ValidConstructorArguments()
    {
        // Build a usable UserManager instance using lightweight mocked collaborators.
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = Mock.Of<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizer = Mock.Of<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>();
        var userManager = new UserManager<IdentityUser<Guid>>(store, identityOptions, passwordHasher, userValidators, passwordValidators, lookupNormalizer, errorDescriber, services, logger);
        // Case 1: ApplicationDbContext with default options (no DB provider)
        var mockedDbContext = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>());
        // Case 2: concrete ApplicationDbContext with default options (no DB provider configured).
        var realOptions = new DbContextOptions<ApplicationDbContext>();
        var realDbContext = new ApplicationDbContext(realOptions);
        return new TheoryData<ApplicationDbContext, UserManager<IdentityUser<Guid>>>
        {
            { mockedDbContext, userManager },
            { realDbContext, userManager },
        };
    }

    /// <summary>
    /// The constructor should create an instance when provided with valid, non-null dependencies.
    /// Inputs:
    /// - dbContext: an ApplicationDbContext (mocked or real).
    /// - userManager: a constructed UserManager&lt;IdentityUser&lt;Guid&gt; &gt;.
    /// Expected:
    /// - A non-null RenamePasskeyModel instance is produced.
    /// - The Input bind property is null by default.
    /// </summary>
    [Theory]
    [MemberData(nameof(ValidConstructorArguments))]
    public void Constructor_ValidDependencies_CreatesInstance(ApplicationDbContext dbContext, UserManager<IdentityUser<Guid>> userManager)
    {
        // Arrange is performed by MemberData.
        // Act
        var model = new RenamePasskeyModel(userManager, dbContext);
        // Assert
        Assert.NotNull(model);
        Assert.IsType<RenamePasskeyModel>(model);
        // The Input property is initialized with a default InputModel instance by the property initializer.
        Assert.NotNull(model.Input);
    }

    /// <summary>
    /// Verifies that multiple constructions with the same dependencies produce distinct RenamePasskeyModel instances.
    /// Inputs:
    /// - A mocked ApplicationDbContext and a constructed UserManager instance.
    /// Expected:
    /// - Two separate instances are returned (reference inequality).
    /// </summary>
    [Fact]
    public void Constructor_MultipleInvocations_ReturnsDistinctInstances()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = Mock.Of<IPasswordHasher<IdentityUser<Guid>>>();
        var userManager = new UserManager<IdentityUser<Guid>>(store, identityOptions, passwordHasher, [], [], Mock.Of<ILookupNormalizer>(), new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());
        var dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().Options);
        // Act
        var first = new RenamePasskeyModel(userManager, dbContext);
        var second = new RenamePasskeyModel(userManager, dbContext);
        // Assert
        Assert.NotSame(first, second);
        Assert.NotNull(first);
        Assert.NotNull(second);
    }
}
