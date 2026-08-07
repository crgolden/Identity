namespace Identity.Extensions;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

public static class UserManagerExtensions
{
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