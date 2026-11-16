namespace Identity.Tests.Unit.Infrastructure;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

internal static class MockHelpers
{
    public static Mock<UserManager<IdentityUser<Guid>>> MockUserManager() =>
        MockUserManager(new IdentityOptions());

    public static Mock<UserManager<IdentityUser<Guid>>> MockUserManager(IdentityOptions identityOptions)
    {
        var options = new Mock<IOptions<IdentityOptions>>(MockBehavior.Strict);
        options.Setup(o => o.Value).Returns(identityOptions);
        var userManager = new Mock<UserManager<IdentityUser<Guid>>>(
            MockBehavior.Strict,
            new Mock<IUserStore<IdentityUser<Guid>>>(MockBehavior.Strict).Object,
            options.Object,
            new Mock<IPasswordHasher<IdentityUser<Guid>>>(MockBehavior.Strict).Object,
            Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            new Mock<ILookupNormalizer>(MockBehavior.Strict).Object,
            new IdentityErrorDescriber(),
            ServiceProviderWithNoServices(),
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);
        userManager.SetupProperty(u => u.Logger);
        userManager.Setup(u => u.GetClaimsAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(new List<Claim>());
        userManager.Setup(u => u.AddClaimsAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        return userManager;
    }

    public static Mock<SignInManager<IdentityUser<Guid>>> MockSignInManager(UserManager<IdentityUser<Guid>> userManager)
    {
        var options = new Mock<IOptions<IdentityOptions>>(MockBehavior.Strict);
        options.Setup(o => o.Value).Returns(new IdentityOptions());
        var signInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            MockBehavior.Strict,
            userManager,
            new Mock<IHttpContextAccessor>(MockBehavior.Strict).Object,
            new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(MockBehavior.Strict).Object,
            options.Object,
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict).Object,
            new Mock<IUserConfirmation<IdentityUser<Guid>>>(MockBehavior.Strict).Object);
        signInManager.SetupProperty(s => s.Logger);
        return signInManager;
    }

    public static IServiceProvider ServiceProviderWithNoServices()
    {
        var services = new Mock<IServiceProvider>(MockBehavior.Strict);
        services.Setup(s => s.GetService(It.IsAny<Type>())).Returns((object?)null);
        return services.Object;
    }

    public static PageContext PageContext(ClaimsPrincipal? user = null) =>
        new()
        {
            HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) },
        };

    public static Mock<RoleManager<IdentityRole<Guid>>> MockRoleManager()
    {
        var roleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            MockBehavior.Strict,
            new Mock<IRoleStore<IdentityRole<Guid>>>(MockBehavior.Strict).Object,
            new List<IRoleValidator<IdentityRole<Guid>>>(),
            new Mock<ILookupNormalizer>(MockBehavior.Strict).Object,
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole<Guid>>>.Instance);
        roleManager.SetupProperty(r => r.Logger);
        return roleManager;
    }

    public static IdentityUser<Guid> TestUser()
    {
        var emailAddress = Generated.NewEmailAddress();
        return new IdentityUser<Guid> { Id = Generated.NewUserId(), UserName = emailAddress, Email = emailAddress };
    }
}
