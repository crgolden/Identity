namespace Identity.Tests.Infrastructure;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>Shared builders for the ASP.NET Core Identity mocks the page-model unit tests depend on.</summary>
internal static class MockHelpers
{
    public static Mock<UserManager<IdentityUser<Guid>>> MockUserManager() =>
        MockUserManager(new IdentityOptions());

    public static Mock<UserManager<IdentityUser<Guid>>> MockUserManager(IdentityOptions identityOptions)
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(identityOptions);
        return new Mock<UserManager<IdentityUser<Guid>>>(
            new Mock<IUserStore<IdentityUser<Guid>>>().Object,
            options.Object,
            new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object,
            new List<IUserValidator<IdentityUser<Guid>>>(),
            new List<IPasswordValidator<IdentityUser<Guid>>>(),
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);
    }

    public static Mock<SignInManager<IdentityUser<Guid>>> MockSignInManager(UserManager<IdentityUser<Guid>> userManager)
    {
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());
        return new Mock<SignInManager<IdentityUser<Guid>>>(
            userManager,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object,
            options.Object,
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object);
    }

    public static PageContext PageContext(ClaimsPrincipal? user = null) =>
        new()
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) },
        };

    public static IdentityUser<Guid> TestUser() =>
        new() { Id = Guid.NewGuid(), UserName = "test@example.com", Email = "test@example.com" };
}
