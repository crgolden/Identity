namespace Identity.Avatar;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

public static class AvatarEndpoints
{
    internal const string RateLimiterPolicyName = "avatar";
    internal const string RoutePattern = "/avatar/{sub}";

    public static IEndpointRouteBuilder MapAvatarEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(RoutePattern, GetAvatarAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimiterPolicyName)
            .WithName("Avatar");

        return endpoints;
    }

    private static async Task<IResult> GetAvatarAsync(
        string sub,
        UserManager<IdentityUser<Guid>> userManager,
        IAvatarService avatarService,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(sub);
        if (user is null)
        {
            return Results.NotFound();
        }

        var claims = await userManager.GetClaimsAsync(user);
        var stored = claims.FirstOrDefault(
            x => string.Equals(x.Type, AvatarProfileService.PictureClaimType, StringComparison.Ordinal)
                && !avatarService.IsOwnComputedUrl(x.Value));
        if (stored is not null && Uri.TryCreate(stored.Value, UriKind.Absolute, out var storedUrl))
        {
            return Results.Redirect(storedUrl.ToString());
        }

        var email = await userManager.GetEmailAsync(user) ?? await userManager.GetUserNameAsync(user);
        if (IsNullOrWhiteSpace(email))
        {
            return Results.NotFound();
        }

        var avatarUrl = await avatarService.GetAvatarUrlAsync(email, cancellationToken);
        return avatarUrl is null ? Results.NotFound() : Results.Redirect(avatarUrl.ToString());
    }
}
