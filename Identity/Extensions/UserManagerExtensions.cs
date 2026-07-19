namespace Identity.Extensions;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

/// <summary>Extension methods for <see cref="UserManager{TUser}"/>.</summary>
public static class UserManagerExtensions
{
    /// <summary>
    /// Adds each claim from <paramref name="principal"/> whose claim type the user does not already have.
    /// Claim types the user already carries are left untouched — this never overwrites an existing value.
    /// </summary>
    /// <remarks>
    /// <see cref="ClaimTypes.NameIdentifier"/> is always excluded: it's the same claim type ASP.NET Core
    /// Identity's <c>UserClaimsPrincipalFactory</c> uses for the user's own ID (added to every principal
    /// before any stored <c>AspNetUserClaims</c> rows, with no dedup against them), so persisting the
    /// provider's subject identifier under that type would give the user two claims of the same type on
    /// every principal built for them. The provider's subject identifier already has a home in
    /// <c>AspNetUserLogins.ProviderKey</c>, populated by <see cref="UserManager{TUser}.AddLoginAsync"/>.
    /// </remarks>
    /// <param name="userManager">The user manager.</param>
    /// <param name="user">The user to add claims to.</param>
    /// <param name="principal">The external login provider's authenticated principal.</param>
    public static async Task AddMissingClaimsAsync(
        this UserManager<IdentityUser<Guid>> userManager,
        IdentityUser<Guid> user,
        ClaimsPrincipal principal)
    {
        var existingTypes = (await userManager.GetClaimsAsync(user))
            .Select(c => c.Type)
            .ToHashSet(StringComparer.Ordinal);
        var missingClaims = principal.Claims
            .Where(c => !string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.Ordinal))
            .Where(c => existingTypes.Add(c.Type))
            .ToList();
        if (missingClaims.Count > 0)
        {
            await userManager.AddClaimsAsync(user, missingClaims);
        }
    }
}
