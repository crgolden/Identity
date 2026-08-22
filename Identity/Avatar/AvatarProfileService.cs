namespace Identity.Avatar;

using System.Security.Claims;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;

public class AvatarProfileService : ProfileService<IdentityUser<Guid>>
{
    internal const string PictureClaimType = "picture";

    private readonly IAvatarService _avatarService;

    public AvatarProfileService(
        UserManager<IdentityUser<Guid>> userManager,
        IUserClaimsPrincipalFactory<IdentityUser<Guid>> claimsFactory,
        IAvatarService avatarService)
        : base(userManager, claimsFactory)
    {
        ThrowIfNull(avatarService);
        _avatarService = avatarService;
    }

    protected override async Task GetProfileDataAsync(ProfileDataRequestContext context, IdentityUser<Guid> user)
    {
        var principal = await GetUserClaimsAsync(user);
        var claims = principal.Claims.ToList();

        var pictureRequested = context.RequestedClaimTypes.Contains(PictureClaimType, StringComparer.Ordinal);
        var storedPicture = claims.Find(
            claim => string.Equals(claim.Type, PictureClaimType, StringComparison.Ordinal)
                && !_avatarService.IsOwnComputedUrl(claim.Value));

        if (pictureRequested && storedPicture is null && (user.Email ?? user.UserName) is { Length: > 0 } identifier)
        {
            claims.RemoveAll(
                claim => string.Equals(claim.Type, PictureClaimType, StringComparison.Ordinal));

            var avatarUrl = await _avatarService.GetAvatarUrlAsync(identifier);
            if (avatarUrl is not null)
            {
                claims.Add(new Claim(PictureClaimType, avatarUrl.AbsoluteUri));
            }
        }

        context.AddRequestedClaims(claims);
    }
}
